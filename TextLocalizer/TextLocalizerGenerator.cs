using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using TextLocalizer.Parsing;
using TextLocalizer.Translations;

namespace TextLocalizer;

[Generator]
internal class TextLocalizerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var settings = context.AnalyzerConfigOptionsProvider
            .Select((config, _) => GeneratorSettings.ReadFromConfig(config.GlobalOptions));

        context.RegisterPostInitializationOutput(AddAttributeTypes);

        var translationFiles = PrepareTranslationFiles(context);
        var translationProviders = PrepareTranslationProvidersData(context);
        var translationTable = LocateTranslationTableClass(context);

        var combinedData = translationFiles
            .Combine(translationProviders)
            .Combine(translationTable)
            .Combine(settings)
            .Select(static (tuple, _) =>
            {
                var (((files, providers), table), settings) = tuple;
                return new CombinedGeneratorData(files, providers, table, settings);
            });

        var aggregatedData = combinedData
            .Select((data, _) => AggregateData(data));

        context.RegisterSourceOutput(
            aggregatedData.Combine(settings),
            static (productionContext, data) => GenerateClasses(data, productionContext)
        );
    }

    private static IncrementalValueProvider<ImmutableArray<ParsedTranslationFile>> PrepareTranslationFiles(IncrementalGeneratorInitializationContext context)
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

    private static IncrementalValueProvider<ImmutableArray<TranslationProviderAttributeData>> PrepareTranslationProvidersData(IncrementalGeneratorInitializationContext context)
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

    private static IncrementalValueProvider<TranslationTableAttributeData?> LocateTranslationTableClass(IncrementalGeneratorInitializationContext context)
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

    private static Dictionary<string, AggregatedTranslationData> AggregateData(CombinedGeneratorData generatorData)
    {
        var settings = generatorData.GeneratorSettings;
        var translations = new Dictionary<string, AggregatedTranslationData>(generatorData.TranslationProviders.Length);

        foreach (var translationFile in generatorData.TranslationFiles)
        {
            var fileLanguage = translationFile.Language;

            if (!translations.ContainsKey(fileLanguage))
            {
                var isDefault = string.Equals(fileLanguage, settings.DefaultLanguage, StringComparison.InvariantCultureIgnoreCase);

                var providerAttribute = generatorData.TranslationProviders
                    .First(x => x.Language == fileLanguage);

                var provider = new ProviderData(providerAttribute.Namespace, providerAttribute.ClassName, isDefault);
                var translationData = new AggregatedTranslationData(translationFile.Language, isDefault, provider);

                translations.Add(translationFile.Language, translationData);
            }

            var module = new TranslationModule(translationFile.ModuleName, translationFile.Path);
            translations[fileLanguage].Modules[translationFile.ModuleName] = module;

            foreach (var entry in translationFile.Entries)
            {
                var text = new TranslationText(entry.Key, entry.Value, null, entry.Line, entry.IsUntranslatable, entry.IsTemplated);
                module.Texts[entry.Key] = text;
            }
        }

        return translations;
    }

    private static void GenerateClasses((Dictionary<string, AggregatedTranslationData>, GeneratorSettings) tuple, SourceProductionContext context)
    {
        var (translations, settings) = tuple;

        var builder = new StringBuilder();

        foreach (var translation in translations.Values)
        {
            var provider = translation.ProviderData;
            var hintName = $"{provider.ClassName}.g.cs";
            var dictionary = new TranslationDictionary2();

            foreach (var module in translation.Modules.Values)
            {
                var moduleDictionary = dictionary[module.Name] = new Dictionary<string, TranslationText>();
                foreach (var text in module.Texts.Values)
                {
                    moduleDictionary[text.Key] = text;
                }
            }

            var providerCode = SourceGenerationHelper.GenerateProvider(builder, settings, provider, dictionary);



            // var builder = new StringBuilder();
            // builder.Append("/*\n");
            //
            // builder.Append("Generate XML: ").AppendLine(settings.GenerateXmlDocs.ToString());
            // builder.Append("Generate ID class: ").AppendLine(settings.GenerateIdClass.ToString());
            //
            // var provider = translation.ProviderData;
            // builder.Append(provider.Namespace).Append(" : ").AppendLine(provider.ClassName);
            //
            // builder.Append("Language: ").AppendLine(translation.Language);
            // builder.Append("Is default: ").AppendLine(translation.IsDefault.ToString());
            //
            // foreach (var module in translation.Modules.Values)
            // {
            //     builder.Append("Module ").AppendLine(module.Name);
            //
            //     foreach (var text in module.Texts.Values)
            //     {
            //         builder.Append("     ").Append(text.Key).Append(": ").AppendLine(text.Value);
            //     }
            // }
            //
            // builder.Append("*/");

            context.AddSource(hintName, providerCode);
        }
    }

    // private static void GenerateClasses((ImmutableArray<TranslationsProviderData>, GeneratorSettings) tuple, SourceProductionContext context)
    // {
        // var (providerData, settings) = tuple;
        //
        // TranslationDictionary? defaultDictionary = null;
        // TranslationsProviderData? defaultProvider = null;
        //
        // var translationsAggregatedByFilename = new Dictionary<string, Dictionary<string, string>>();
        // var nonDefaultTranslations = new Dictionary<TranslationsProviderData, Dictionary<string, LocalizedText>>();
        //
        // var builder = new StringBuilder();
        //
        // foreach (var data in providerData)
        // {
        //     var parsed = FileParser.Parse(data.File);
        //
        //     // Aggregating translations for generating xml docs
        //     foreach (var pair in parsed)
        //     {
        //         var (key, localizedText) = (pair.Key, pair.Value);
        //         if (!translationsAggregatedByFilename.TryGetValue(key, out var translations))
        //         {
        //             translations = new Dictionary<string, string>();
        //             translationsAggregatedByFilename[key] = translations;
        //         }
        //
        //         translations[data.File.Name] = localizedText.Text;
        //     }
        //
        //     // if (data.ProviderAttributeData.IsDefault.Value)
        //     // {
        //     //     defaultProvider = data;
        //     //     defaultDictionary = SourceGenerationHelper.CreateIndexedLocalizedTextDictionary(parsed);
        //     // }
        //     // else
        //     // {
        //     //     nonDefaultTranslations[data] = parsed;
        //     // }
        // }
        //
        // if (defaultDictionary is null)
        // {
        //     return;
        // }
        //
        // foreach (var pair in nonDefaultTranslations)
        // {
        //     var (data, translation) = (pair.Key, pair.Value);
        //
        //     var indexedTranslations = IndexTranslationKeys(defaultDictionary, translation, data, context);
        //     var result = SourceGenerationHelper.GenerateProvider(builder, data.ProviderAttributeData, indexedTranslations);
        //     context.AddSource($"{data.ProviderAttributeData.ClassName}.g.cs", result);
        // }
        //
        // if (defaultProvider is not { Table: { } tableData } provider)
        // {
        //     return;
        // }
        //
        // // Generate default provider and table with XML docs
        // var providerClass = provider.ProviderAttributeData;
        // var generatedDefaultProvider = SourceGenerationHelper.GenerateProvider(builder, providerClass, defaultDictionary);
        // context.AddSource($"{providerClass.ClassName}.g.cs", generatedDefaultProvider);
        //
        // var generatedTable = SourceGenerationHelper.GenerateLocalizationTable(builder, settings, tableData, defaultDictionary, translationsAggregatedByFilename);
        // context.AddSource($"{tableData.ClassName}.g.cs", generatedTable);
        //
        // if (settings.GenerateIdClass && SyntaxFacts.IsValidIdentifier(settings.IdClassName))
        // {
        //     var generatedIdClass = SourceGenerationHelper.GenerateIdClass(builder, settings, tableData, defaultDictionary, translationsAggregatedByFilename);
        //     context.AddSource($"{settings.IdClassName}.g.cs", generatedIdClass);
        // }
    // }

    private static TranslationDictionary IndexTranslationKeys(
        Dictionary<string, IndexedLocalizedText> defaultTable,
        Dictionary<string, LocalizedText> localizedTable,
        TranslationsProviderData providerData,
        SourceProductionContext context)
    {
        var indexedTranslations = new TranslationDictionary();

        foreach (var pair in defaultTable)
        {
            var (key, defaultValue) = (pair.Key, pair.Value);

            if (localizedTable.TryGetValue(key, out var localizedValue))
            {
                if (defaultValue.IsUntranslatable)
                {
                    context.ReportUntranslatableKeyDiagnostic(providerData, key, localizedValue.LineNumber);
                }
                else
                {
                    indexedTranslations[key] = new IndexedLocalizedText(localizedValue.Text, defaultValue.Index, localizedValue.IsUntranslatable);
                }
            }
            else if (!defaultValue.IsUntranslatable)
            {
                context.ReportMissingKeyDiagnostic(providerData, key);
            }
        }

        foreach (var pair in localizedTable)
        {
            if (!defaultTable.ContainsKey(pair.Key))
            {
                context.ReportExtraKeyDiagnostic(providerData, pair.Key, pair.Value.LineNumber);
            }
        }

        return indexedTranslations;
    }

    // private static ImmutableArray<SourceGeneratorData> CombineProvidersWithFiles(
    //     ImmutableArray<TranslationProviderAttributeData?> classes,
    //     ImmutableArray<TranslationsFileData> files)
    // {
    //     var builder = ImmutableArray.CreateBuilder<SourceGeneratorData>();
    //
    //     var fileLookup = files.ToDictionary(static x => x.Name);
    //     foreach (var classInfo in classes)
    //     {
    //         if (classInfo is not { } validClassInfo) continue;
    //
    //         if (fileLookup.TryGetValue(validClassInfo.Language.Value, out var matchingFile))
    //         {
    //             builder.Add(new SourceGeneratorData(validClassInfo, matchingFile));
    //         }
    //     }
    //
    //     return builder.ToImmutable();
    // }
    //
    // private static ImmutableArray<TranslationsProviderData> CreateProviderTableData(
    //     ImmutableArray<SourceGeneratorData> providerData,
    //     ImmutableArray<TranslationTableAttributeData?> tableData)
    // {
    //     var table = tableData.Single();
    //     var builder = ImmutableArray.CreateBuilder<TranslationsProviderData>();
    //
    //     foreach (var combinedProviderData in providerData)
    //     {
    //         builder.Add(new TranslationsProviderData(combinedProviderData.TranslationProviderAttributeData, combinedProviderData.TranslationsFile, table));
    //     }
    //
    //     return builder.ToImmutable();
    // }

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
            SourceText.From(SourceGenerationHelper.ProviderAttribute, Encoding.UTF8)
        );

        context.AddSource(
            "LocalizationTableAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.LocalizationTableAttribute, Encoding.UTF8)
        );
    }
}
