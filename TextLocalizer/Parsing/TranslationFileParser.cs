using System.Text.Json;
using TextLocalizer.Pipeline;

namespace TextLocalizer.Parsing;

internal static class TranslationParser
{
    public static TranslationFile? Parse(TranslationsFileData file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.Path);

        return extension switch
        {
            ".json" => ParseJson(file, cancellationToken),
            // ".yml" or ".yaml" => ParseYaml(file),
            // ".xml" => ParseXml(file),
            _ => null
        };
    }

    private static (string language, string module) ResolveMetadata(TranslationsFileData file)
    {
        var directory = Path.GetDirectoryName(file.Path)!;
        var language = Path.GetFileName(directory);

        var module = Path.GetFileNameWithoutExtension(file.Path);

        return (language, module);
    }

    private static TranslationFile? ParseJson(TranslationsFileData file, CancellationToken cancellationToken)
    {
        var (language, module) = ResolveMetadata(file);

        var entries = new List<TranslationEntry>();

        using var doc = JsonDocument.Parse(file.SourceText.ToString());

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var entry = new TranslationEntry(
                prop.Name,
                prop.Value.ToString(),
                Description: null,
                Line: 0,
                IsTemplated: prop.Name == "online",
                IsUntranslatable: false);

            entries.Add(entry);
        }

        return new TranslationFile(language, module, file.Path, entries);
    }
}
