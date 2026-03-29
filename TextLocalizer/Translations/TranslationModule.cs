namespace TextLocalizer.Translations;

public record TranslationModule<TKey>(string Name, string SourceFilePath) where TKey : IEquatable<TKey>
{
    private readonly Dictionary<TKey, TranslationText<TKey>> _texts = [];

    public TranslationText<TKey> this[TKey key]
    {
        get => _texts[key];
        set => _texts[key] = value;
    }

    public bool ContainsEntry(TKey key) => _texts.ContainsKey(key);

    public void AddText(TranslationText<TKey> entry) => _texts.Add(entry.Key, entry);

    public IReadOnlyCollection<TranslationText<TKey>> Texts => _texts.Values;
}
