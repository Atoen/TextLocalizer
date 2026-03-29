using Microsoft.CodeAnalysis.Text;

namespace TextLocalizer.Pipeline;

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