using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using TextLocalizer.Parsing;
using TextLocalizer.Translations;
using TextLocalizer.Types;

namespace TextLocalizer;

[Generator]
internal class TextLocalizerGenerator : IIncrementalGenerator
{
    internal static readonly StringBuilder ErrorBuilder = new();
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

    private static void PipeLine(IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<GeneratorSettings> settings)
    {
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
            aggregatedData,
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

    private static Data2<string> AggregateData(CombinedGeneratorData generatorData)
    {
        var translations = new Dictionary<string, AggregatedTranslationData<string>>(generatorData.TranslationProviders.Length);
        var settings = generatorData.GeneratorSettings;
        
        foreach (var translationFile in generatorData.TranslationFiles)
        {
            var fileLanguage = translationFile.Language;

            if (!translations.ContainsKey(fileLanguage))
            {
                var isDefault = string.Equals(fileLanguage, settings.DefaultLanguage, StringComparison.InvariantCultureIgnoreCase);

                var providerAttribute = generatorData.TranslationProviders
                    .First(x => x.Language == fileLanguage);

                var provider = new ProviderData(providerAttribute.Namespace, providerAttribute.ClassName, isDefault);
                var translationData = new AggregatedTranslationData<string>(translationFile.Language, isDefault, provider);

                translations.Add(translationFile.Language, translationData);
            }

            var module = new TranslationModule<string>(translationFile.ModuleName, translationFile.Path);
            translations[fileLanguage].Modules[translationFile.ModuleName] = module;

            foreach (var entry in translationFile.Entries)
            {
                var text = new TranslationText(entry.Key, entry.Value, null, entry.Line, entry.IsUntranslatable, entry.IsTemplated);
                module.Texts[entry.Key] = text;
            }
        }

        var table = new TableData(
            generatorData.TranslationTable?.Namespace,
            generatorData.TranslationTable?.ClassName,
            generatorData.TranslationTable?.TableName);

        return new Data2<string>(table, translations, settings);
    }

    // private static Data2<int> IndexData(Data2<string> data, SourceProductionContext context)
    // {
    //     var defaultTranslations = data.Translations.Single(x => x.Value.IsDefault).Value;
    //     var nonDefaultTranslations = data.Translations
    //         .Where(x => !x.Value.IsDefault)
    //         .Select(x => x.Value)
    //         .ToArray();
    //     
    //     var indexedTranslations = new Dictionary<string, AggregatedTranslationData<int>>(data.Translations.Count);
    //         
    //     var indexedDefault = defaultTranslations.ToKey<int>();
    //
    //     var index = 1;
    //     var moduleMissing = false;
    //     foreach (var module in defaultTranslations.Modules.Values)
    //     {
    //         var indexedModule = new TranslationModule<int>(module.Name, module.SourceFilePath);
    //         indexedDefault.Modules[module.Name] = indexedModule;
    //
    //         foreach (var translation in nonDefaultTranslations)
    //         {
    //             if (!translation.Modules.ContainsKey(indexedModule.Name))
    //             {
    //                 // missing module
    //                 //context.ReportMissingModuleDiagnostic("", indexedDefault.Language, indexedModule.Name);
    //                 moduleMissing = true;
    //             }
    //         }
    //         
    //         foreach (var text in module.Texts.Values)
    //         {
    //             var resourceId = index++;
    //
    //             indexedModule.Texts[resourceId] = text;
    //
    //             if (!moduleMissing)
    //             {
    //                 foreach (var translation in nonDefaultTranslations)
    //                 {
    //                     if (!translation.Modules[indexedModule.Name].Texts.ContainsKey(text.Key))
    //                     {
    //                         context.ReportMissingKeyDiagnostic(
    //                             translation.Modules[indexedModule.Name].SourceFilePath,
    //                             translation.Modules[indexedModule.Name].Name,
    //                             text.Key
    //                             );
    //                     }
    //                 }
    //             }
    //         }
    //     }
    //
    //     indexedTranslations[indexedDefault.Language] = indexedDefault;
    //
    //     foreach (var translations in data.Translations.Values)
    //     {
    //         if (translations.IsDefault) continue;
    //         
    //         var indexed = translations.ToKey<int>();
    //
    //         foreach (var module in indexedDefault.Modules.Values)
    //         {
    //             var a = indexed.Modules[module.Name] = new TranslationModule<int>(module.Name, module.SourceFilePath);
    //             
    //             foreach (var textKvp in module.Texts)
    //             {
    //                 if (translations.Modules[module.Name].Texts.TryGetValue(textKvp.Value.Key, out var dd))
    //                 {
    //                     var id = textKvp.Key;
    //                     a.Texts[id] = dd;
    //                 }
    //             }
    //         }
    //     }
    //
    //     return new Data2<int>(data.TableData, indexedTranslations, data.GeneratorSettings);
    // }
    
    private static Data2<int> IndexData(Data2<string> data, SourceProductionContext context)
{
    var defaultTranslations = data.Translations.Single(x => x.Value.IsDefault).Value;
    var nonDefaultTranslations = data.Translations
        .Where(x => !x.Value.IsDefault)
        .Select(x => x.Value)
        .ToArray();

    var indexedTranslations = new Dictionary<string, AggregatedTranslationData<int>>(data.Translations.Count);

    var indexedDefault = defaultTranslations.ToKey<int>();

    // Key → ID map per module
    var moduleKeyIndex = new Dictionary<string, Dictionary<string, int>>();

    var nextId = 1;

    foreach (var module in defaultTranslations.Modules.Values)
    {
        var indexedModule = new TranslationModule<int>(module.Name, module.SourceFilePath);
        indexedDefault.Modules[module.Name] = indexedModule;

        var keyMap = moduleKeyIndex[module.Name] = new Dictionary<string, int>();

        // Validate module existence early
        foreach (var translation in nonDefaultTranslations)
        {
            if (!translation.Modules.ContainsKey(module.Name))
            {
                // context.ReportMissingModuleDiagnostic(
                //     translation.SourceFilePath,
                //     translation.Language,
                //     module.Name);
            }
        }

        foreach (var text in module.Texts.Values)
        {
            var id = nextId++;

            keyMap[text.Key] = id;
            indexedModule.Texts[id] = text;

            // Validate key existence
            foreach (var translation in nonDefaultTranslations)
            {
                if (translation.Modules.TryGetValue(module.Name, out var trModule))
                {
                    if (!trModule.Texts.ContainsKey(text.Key))
                    {
                        context.ReportMissingKeyDiagnostic(
                            trModule.SourceFilePath,
                            trModule.Name,
                            text.Key);
                    }
                }
            }
        }
    }

    indexedTranslations[indexedDefault.Language] = indexedDefault;

    // Rebuild non-default translations using ID map
    foreach (var translations in nonDefaultTranslations)
    {
        var indexed = translations.ToKey<int>();

        foreach (var defaultModule in indexedDefault.Modules.Values)
        {
            var rebuiltModule = indexed.Modules[defaultModule.Name] =
                new TranslationModule<int>(defaultModule.Name, defaultModule.SourceFilePath);

            if (!translations.Modules.TryGetValue(defaultModule.Name, out var sourceModule))
                continue;

            var keyMap = moduleKeyIndex[defaultModule.Name];

            foreach (var text in sourceModule.Texts.Values)
            {
                if (keyMap.TryGetValue(text.Key, out var id))
                {
                    rebuiltModule.Texts[id] = text;
                }
            }
        }

        indexedTranslations[indexed.Language] = indexed;
    }

    return new Data2<int>(data.TableData, indexedTranslations, data.GeneratorSettings);
}
    
    private static void GenerateClasses(Data2<string> data, SourceProductionContext context)
    {
        //   Generator „TextLocalizerGenerator” nie mógł wygenerować źródła. W rezultacie nie będzie on współtworzyć danych wyjściowych i mogą wystąpić błędy kompilacji. Wyjątek był typu „FileNotFoundException” z komunikatem „Could not load file or assembly 'TextLocalizer.Types, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null'. Nie można odnaleźć określonego pliku.”.

        var data2 = IndexData(data, context);
        
        var translations = data2.Translations;
        var settings = data2.GeneratorSettings;
        var table = data2.TableData;
        
        if (!translations.TryGetValue(settings.DefaultLanguage, out var defaultTranslation))
        {
            ErrorBuilder.Append("No default translation found");
            return;
        }
        
        var builder = new StringBuilder();
        
        var translationTableCode = SourceGenerationHelper.GenerateTranslationTable(builder, settings, table, defaultTranslation);
        context.AddSource("TranslationTable.g.cs", translationTableCode);
        
        foreach (var translation in translations.Values)
        {
            var provider = translation.ProviderData;
            var hintName = $"{provider.ClassName}.g.cs";
            var dictionary = new Dictionary<string, Dictionary<int, TranslationText>>();
        
            foreach (var module in translation.Modules.Values)
            {
                var moduleDictionary = dictionary[module.Name] = new Dictionary<int, TranslationText>();
                foreach (var textKvp in module.Texts)
                {
                    moduleDictionary[textKvp.Key] = textKvp.Value;
                }
            }
        
            var providerCode = SourceGenerationHelper.GenerateProvider(builder, settings, provider, dictionary);
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
                    // context.ReportUntranslatableKeyDiagnostic(providerData, key, localizedValue.LineNumber);
                }
                else
                {
                    indexedTranslations[key] = new IndexedLocalizedText(localizedValue.Text, defaultValue.Index, localizedValue.IsUntranslatable);
                }
            }
            else if (!defaultValue.IsUntranslatable)
            {
                // context.ReportMissingKeyDiagnostic(providerData, key);
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
