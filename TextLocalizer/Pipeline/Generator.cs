using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using TextLocalizer.Diagnostics;
using TextLocalizer.Parsing;
using TextLocalizer.Pipeline.Builders;
using TextLocalizer.Translations;

// [assembly: TypeForwardedTo(typeof(TranslationEntry))]

namespace TextLocalizer.Pipeline;

[Generator]
internal class Generator : IIncrementalGenerator
{
    internal readonly static StringBuilder ErrorBuilder = new();
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();

        ErrorBuilder.Clear();
        
        var settings = context.AnalyzerConfigOptionsProvider
            .Select((config, _) => GeneratorSettings.ReadFromConfig(config.GlobalOptions));
        
        try
        {
            PipeLine(context, settings);
        }
        catch (Exception e)
        {
            ErrorBuilder.Append(e);
            context.RegisterSourceOutput(settings, static (productionContext, settings) =>
            {
                productionContext.AddSource("Errors.g.cs", "/*" + ErrorBuilder + settings + "*/");
            });
        }

        if (ErrorBuilder.Length > 0)
        {
            context.RegisterSourceOutput(settings, static (productionContext, settings) =>
            {
                productionContext.AddSource("Errors.g.cs", "/*" + ErrorBuilder + settings + "*/");
            });
        }
    }

    private static void PipeLine(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<GeneratorSettings> settings)
    {
        context.RegisterPostInitializationOutput(AddAttributeTypes);

        var translationFiles = PrepareTranslationFiles(context);
        var translationProviders = PrepareTranslationProvidersData(context);
        var translationTable = LocateTranslationTableClass(context);

        var pipelineData = translationFiles
            .Combine(translationProviders)
            .Combine(translationTable)
            .Combine(settings)
            .Select(static (tuple, _) =>
            {
                var (((files, providers), table), settings) = tuple;
                return new PipelineData(files, providers, table, settings);
            });

        // var aggregatedData = combinedData
        //     .Select((data, _) => AggregateData(data))
        //     .Select((data, _) => ChangeStringKeysToIds(data));

        var merged = pipelineData
            .Select((data, _) => MergeTranslations(data))
            .Combine(settings);

        context.RegisterSourceOutput(
            merged,
            static (productionContext, data) => GenerateClasses(data, productionContext)
        );
    }

    private static IncrementalValueProvider<ImmutableArray<TranslationFile>> PrepareTranslationFiles(
        IncrementalGeneratorInitializationContext context)
    {
        return context.AdditionalTextsProvider
            .Where(GeneratorSettings.IsSupportedFileType)
            .Select(static (additionalText, cancellationToken) =>
            {
                var path = additionalText.Path;
                var name = Path.GetFileName(path);
                var text = additionalText.GetText(cancellationToken);

                return new TranslationsFileData(path, name, text!);
            })
            .Where(static fileData => fileData.SourceText is { Length: > 0 })
            .Select(static (file, cancellationToken) => TranslationParser.Parse(file, cancellationToken)!)
            .Where(static result => result is not null)
            .Collect();
    }

    private static IncrementalValueProvider<ImmutableArray<TranslationProviderAttributeData>> PrepareTranslationProvidersData(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TextLocalizer.TranslationProviderAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetTextProviderAttributeData(ctx.SemanticModel, ctx.TargetNode))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value)
            .Collect();
    }

    private static IncrementalValueProvider<TranslationTableAttributeData?> LocateTranslationTableClass(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TextLocalizer.LocalizationTableAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetTranslationTableAttributeData(ctx.SemanticModel, ctx.TargetNode))
            .Where(x => x is not null)
            .Collect()
            .Select(static (x, _) => x.FirstOrDefault());
    }

    private static (CombinedTranslations<int> combined, List<TranslationError> errors) MergeTranslations(PipelineData pipelineData)
    {
        var mainLanguage = pipelineData.GeneratorSettings.MainLanguage;
        var providersWithFiles = pipelineData.TranslationProviders
            .GroupJoin(
                pipelineData.TranslationFiles,
                provider => provider.Language,
                file => file.Language,
                (provider, files) => (provider, files.ToArray())
            )
            .OrderByDescending(x => string.Equals(
                x.provider.Language,
                mainLanguage,
                StringComparison.InvariantCultureIgnoreCase))
            .ToArray();

        var errors = new List<TranslationError>();

        var tableAttribute = pipelineData.TranslationTable!.Value;
        var combined = new CombinedTranslations<int>
        {
            Table = new TranslationTable(tableAttribute.Namespace, tableAttribute.ClassName, tableAttribute.TableName, tableAttribute.DefaultProviderAccessor, tableAttribute.CurrentProviderAccessor)
        };

        var keyToIndex = new Dictionary<(string module, string sourceKey), int>();
        var nextIndex = 1;

        foreach (var (providerAttribute, files) in providersWithFiles)
        {
            var isDefault = string.Equals(providerAttribute.Language, mainLanguage, StringComparison.InvariantCultureIgnoreCase);
            var translation = new Translation<int>(providerAttribute.Language, isDefault)
            {
                Provider = new TranslationProvider(providerAttribute.Namespace, providerAttribute.ClassName)
            };

            var seenKeys = isDefault ? null : new HashSet<(string module, string sourceKey)>();

            foreach (var translationFile in files)
            {
                var module = new TranslationModule<int>(translationFile.ModuleName, translationFile.Path);
                foreach (var entry in translationFile.Entries)
                {
                    if (!SyntaxFacts.IsValidIdentifier(entry.Key))
                    {
                        errors.Add(TranslationError.InvalidKey(translation.Language, module.Name, entry.Key, entry.Line));
                        continue;
                    }

                    if (entry.Key == module.Name)
                    {
                        errors.Add(TranslationError.KeyConflictsWithModule(translation.Language, module.Name, entry.Key, entry.Line));
                        continue;
                    }

                    var id = (translationFile.ModuleName, entry.Key);
                    int index;

                    if (isDefault)
                    {
                        index = nextIndex++;
                        keyToIndex.Add(id, index);
                    }
                    else
                    {
                        if (!keyToIndex.TryGetValue(id, out index))
                        {
                            errors.Add(TranslationError.UnexpectedKey(translation.Language, module.Name, entry.Key, entry.Line));
                            continue;
                        }

                        seenKeys?.Add(id);

                        if (entry.IsUntranslatable)
                        {
                            errors.Add(TranslationError.Untranslatable(translation.Language, module.Name, entry.Key, entry.Line));
                            continue;
                        }
                    }

                    var text = new TranslationText<int>(index, entry.Value)
                    {
                        SourceKey = entry.Key,
                        Description = entry.Description,
                        IsTemplated = entry.IsTemplated,
                        IsUntranslatable = entry.IsUntranslatable,
                        Module = module.Name
                    };

                    module.AddText(text);
                }

                translation.AddModule(module);
            }

            if (!isDefault)
            {
                foreach (var expected in keyToIndex.Keys)
                {
                    if (!seenKeys!.Contains(expected))
                    {
                        var (module, key) = expected;
                        errors.Add(TranslationError.KeyMissing(translation.Language, module, key));
                    }
                }
            }

            combined.AddTranslation(translation);
        }

        return (combined, errors);
    }

    private static void GenerateClasses(((CombinedTranslations<int>, List<TranslationError>), GeneratorSettings) tuple, SourceProductionContext context)
    {
        var ((translations, errors), settings) = tuple;

        if (translations is { MainTranslation: null } or { Table: null })
        {
            return;
        }

        foreach (var error in errors)
        {
            context.ReportError(error);
        }

        var builder = new StringBuilder();
        var translationTableCode = TranslationTableBuilder.BuildTranslationTable(builder, settings, translations);
        context.AddSource("TranslationTable.g.cs", translationTableCode);

        if (settings.GenerateIdClass)
        {
            var idClassCode = IdClassBuilder.BuildIdClass(builder, settings, translations);
            context.AddSource("IdClass.g.cs", idClassCode);
        }

        var highestId = translations.MainTranslation.Modules.Last().Texts.Last().Key;

        foreach (var translation in translations.Languages)
        {
            var providerCode = TextProviderBuilder.BuildProvider(builder, highestId, translation);
            context.AddSource($"{translation.Provider.ClassName}.g.cs", providerCode);
        }
    }

    private static TranslationProviderAttributeData? GetTextProviderAttributeData(SemanticModel semanticModel, SyntaxNode classDeclarationSyntax)
    {
        return semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol classSymbol
            ? SourceGenerationHelper.CreateTranslationProviderInfo(classSymbol)
            : null;
    }

    private static TranslationTableAttributeData? GetTranslationTableAttributeData(SemanticModel semanticModel, SyntaxNode classDeclarationSyntax)
    {
        return semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol classSymbol
            ? SourceGenerationHelper.CreateLocalizationTableData(classSymbol)
            : null;
    }

    private static void AddAttributeTypes(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource(
            "TranslationProviderAttribute.g.cs",
            SourceText.From(AttributeDefinitions.ProviderAttribute, Encoding.UTF8)
        );

        context.AddSource(
            "LocalizationTableAttribute.g.cs",
            SourceText.From(AttributeDefinitions.LocalizationTableAttribute, Encoding.UTF8)
        );
    }
}
