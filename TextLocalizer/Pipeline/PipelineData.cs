using System.Collections.Immutable;
using TextLocalizer.Parsing;

namespace TextLocalizer.Pipeline;

internal readonly record struct PipelineData
{
    public readonly ImmutableArray<TranslationFile> TranslationFiles;
    public readonly ImmutableArray<TranslationProviderAttributeData> TranslationProviders;
    public readonly TranslationTableAttributeData? TranslationTable;
    public readonly GeneratorSettings GeneratorSettings;

    public PipelineData(ImmutableArray<TranslationFile> translationFiles, ImmutableArray<TranslationProviderAttributeData> translationProviders, TranslationTableAttributeData? translationTable, GeneratorSettings generatorSettings)
    {
        TranslationFiles = translationFiles;
        TranslationProviders = translationProviders;
        TranslationTable = translationTable;
        GeneratorSettings = generatorSettings;
    }
}
