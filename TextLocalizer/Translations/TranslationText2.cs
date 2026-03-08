namespace TextLocalizer.Translations;

public record TranslationText2<TKey>(TKey Key, string Value) where TKey : IEquatable<TKey>
{
    public string SourceKey { get; init; }
    public string Module { get; init; }

    public string? Description { get; init; }
    public bool IsUntranslatable { get; init; }
    public bool IsTemplated { get; init; }

    public TranslationText2<T> ChangeKey<T>(T newKey) where T : IEquatable<T> => new(newKey, Value)
    {
        SourceKey = SourceKey,
        Description = Description,
        IsUntranslatable = IsUntranslatable,
        IsTemplated = IsTemplated
    };
}
