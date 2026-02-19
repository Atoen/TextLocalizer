using System.Text.Json;

namespace TextLocalizer.Parsing;

internal static class TranslationParser
{
    public static ParsedTranslationFile? Parse(TranslationsFileData file, CancellationToken cancellationToken)
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

    private static ParsedTranslationFile? ParseJson(TranslationsFileData file, CancellationToken cancellationToken)
    {
        var (language, module) = ResolveMetadata(file);

        var entries = new List<ParsedTranslationEntry>();

        using var doc = JsonDocument.Parse(file.SourceText.ToString());

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            entries.Add(new ParsedTranslationEntry(
                prop.Name,
                prop.Value.ToString() ?? "",
                Line: 0,
                IsTemplated: false,
                IsUntranslatable: false));
        }

        return new ParsedTranslationFile(language, module, file.Path, entries);
    }
}
