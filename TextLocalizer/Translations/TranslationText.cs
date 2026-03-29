namespace TextLocalizer.Translations;

public record TranslationText<TKey>(TKey Key, string Value) where TKey : IEquatable<TKey>
{
    public string SourceKey { get; init; }
    public string Module { get; init; }

    public string? Description { get; init; }
    public bool IsUntranslatable { get; init; }
    public bool IsTemplated { get; init; }
}
