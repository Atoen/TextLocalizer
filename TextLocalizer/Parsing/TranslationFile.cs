using System.Runtime.CompilerServices;

namespace TextLocalizer.Parsing;

internal sealed record TranslationFile(
    string Language,
    string ModuleName,
    string Path,
    IReadOnlyList<TranslationEntry> Entries);

[TypeForwardedFrom("TextLocalizer.Types")]
internal sealed record TranslationEntry(
    string Key,
    string Value,
    int Line,
    string? Description,
    bool IsTemplated,
    bool IsUntranslatable);