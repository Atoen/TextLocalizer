namespace TextLocalizer.Translations;

public record TranslationModule2<TKey>(string Name, string SourceFilePath) where TKey : IEquatable<TKey>
{
    private readonly Dictionary<TKey, TranslationText2<TKey>> _texts = [];

    public TranslationText2<TKey> this[TKey key]
    {
        get => _texts[key];
        set => _texts[key] = value;
    }

    public bool ContainsEntry(TKey key) => _texts.ContainsKey(key);

    public void AddText(TranslationText2<TKey> entry) => _texts.Add(entry.Key, entry);

    public IReadOnlyCollection<TranslationText2<TKey>> Texts => _texts.Values;

    public TranslationModule2<T> ReIndex<T>(Dictionary<TKey, T> keyMap) where T : IEquatable<T>
    {
        var reIndexed = new TranslationModule2<T>(Name, SourceFilePath);

        foreach (var keyValuePair in keyMap)
        {
            var (keyFrom, keyTo) = (keyValuePair.Key, keyValuePair.Value);
            if (_texts.TryGetValue(keyFrom, out var value))
            {
                reIndexed[keyTo] = value.ChangeKey(keyTo);
            }
        }

        return reIndexed;
    }
}
