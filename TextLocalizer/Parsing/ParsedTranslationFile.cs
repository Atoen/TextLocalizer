namespace TextLocalizer.Parsing;

internal sealed record ParsedTranslationFile
{
    public ParsedTranslationFile(string Language,
        string ModuleName,
        string Path,
        IReadOnlyList<ParsedTranslationEntry> Entries)
    {
        this.Language = Language;
        this.ModuleName = ModuleName;
        this.Path = Path;
        this.Entries = Entries;
    }

    public string Language { get; }
    public string ModuleName { get; }
    public string Path { get; }
    public IReadOnlyList<ParsedTranslationEntry> Entries { get; }
}

internal sealed record ParsedTranslationEntry
{
    public ParsedTranslationEntry(string Key,
        string Value,
        int Line,
        bool IsTemplated,
        bool IsUntranslatable)
    {
        this.Key = Key;
        this.Value = Value;
        this.Line = Line;
        this.IsTemplated = IsTemplated;
        this.IsUntranslatable = IsUntranslatable;
    }

    public string Key { get; }
    public string Value { get; }
    public int Line { get; }
    public bool IsTemplated { get; }
    public bool IsUntranslatable { get; }
}