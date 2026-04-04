using System.Text;
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

        var json = file.SourceText.ToString();
        var bytes = Encoding.UTF8.GetBytes(json);

        var reader = new Utf8JsonReader(bytes, isFinalBlock: true, state: default);
        
        var lineStarts = GetLineStarts(bytes);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = reader.GetString()!;
                
                var line = GetLineNumber(reader.TokenStartIndex, lineStarts);
                
                reader.Read();

                var value = string.Empty;
                string? description = null;
                var isTemplated = false;
                var isUntranslatable = false;

                if (reader.TokenType == JsonTokenType.String)
                {
                    value = reader.GetString()!;
                    isTemplated = DetectTemplated(value);
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName)
                            continue;

                        var innerKey = reader.GetString()!;
                        reader.Read();

                        switch (innerKey)
                        {
                            case "value":
                                value = reader.GetString()!;
                                break;
                            case "description":
                                description = reader.GetString();
                                break;
                            case "templated":
                                isTemplated = reader.GetBoolean();
                                break;
                            case "untranslatable":
                                isUntranslatable = reader.GetBoolean();
                                break;
                        }
                    }
                }
                else
                {
                    throw new FormatException($"Invalid value for key '{key}'");
                }

                entries.Add(new TranslationEntry(
                    key,
                    value,
                    Description: description,
                    Line: line,
                    IsTemplated: isTemplated,
                    IsUntranslatable: isUntranslatable));
            }
        }

        return new TranslationFile(language, module, file.Path, entries);
    }
    private static List<long> GetLineStarts(byte[] bytes)
    {
        var result = new List<long> { 0 };

        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == '\n')
                result.Add(i + 1);
        }

        return result;
    }

    private static int GetLineNumber(long position, List<long> lineStarts)
    {
        var line = 0;

        for (var i = 0; i < lineStarts.Count; i++)
        {
            if (lineStarts[i] > position)
                break;

            line = i;
        }

        return line + 1;
    }
    
    private static bool DetectTemplated(string value)
    {
        return value.IndexOf('{') >= 0 && value.IndexOf('}') > value.IndexOf('{');
    }
}
