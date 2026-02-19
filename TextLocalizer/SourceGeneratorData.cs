using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using TextLocalizer.Parsing;

namespace TextLocalizer;

internal readonly record struct TranslationsProviderData
{
    public readonly TranslationProviderAttributeData ProviderAttributeData;
    public readonly TranslationsFileData File;
    public readonly TranslationTableAttributeData? Table;

    public TranslationsProviderData(TranslationProviderAttributeData providerAttributeData, TranslationsFileData file, TranslationTableAttributeData? table)
    {
        ProviderAttributeData = providerAttributeData;
        Table = table;
        File = file;
    }
}

internal readonly record struct SourceGeneratorData
{
    public readonly TranslationProviderAttributeData TranslationProviderAttributeData;
    public readonly TranslationsFileData TranslationsFile;

    public SourceGeneratorData(TranslationProviderAttributeData translationProviderAttributeData, TranslationsFileData translationsFile)
    {
        TranslationProviderAttributeData = translationProviderAttributeData;
        TranslationsFile = translationsFile;
    }
}

internal readonly record struct TranslationsFileData
{
    public readonly string Path;
    public readonly string Name;
    public readonly SourceText SourceText;

    public TranslationsFileData(string path, string name, SourceText sourceText)
    {
        Path = path;
        Name = name;
        SourceText = sourceText;
    }
}

internal readonly record struct CombinedGeneratorData
{
    public readonly ImmutableArray<ParsedTranslationFile> TranslationFiles;
    public readonly ImmutableArray<TranslationProviderAttributeData> TranslationProviders;
    public readonly TranslationTableAttributeData? TranslationTable;
    public readonly GeneratorSettings GeneratorSettings;

    public CombinedGeneratorData(ImmutableArray<ParsedTranslationFile> translationFiles, ImmutableArray<TranslationProviderAttributeData> translationProviders, TranslationTableAttributeData? translationTable, GeneratorSettings generatorSettings)
    {
        TranslationFiles = translationFiles;
        TranslationProviders = translationProviders;
        TranslationTable = translationTable;
        GeneratorSettings = generatorSettings;
    }
}
