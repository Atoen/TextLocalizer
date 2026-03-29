namespace TextLocalizer.Translations;

public record CombinedTranslations<TKey> where TKey : IEquatable<TKey>
{
    private readonly Dictionary<string, Translation<TKey>> _translations = [];

    public TranslationTable? Table { get; init; }

    public Translation<TKey>? MainTranslation { get; private set; }

    public IReadOnlyCollection<Translation<TKey>> Languages => _translations.Values;

    public void AddTranslation(Translation<TKey> translation) => this[translation.Language] = translation;

    public Translation<TKey> this[string language]
    {
        get => _translations[language];
        set
        {
            if (value.IsDefault)
            {
                MainTranslation = value;
            }

            _translations[language] = value;
        }
    }
}
