namespace TextLocalizer.Translations;

public record Translation<TKey>(string Language, bool IsDefault) where TKey : IEquatable<TKey>
{
    private readonly Dictionary<string, TranslationModule2<TKey>> _modules = [];

    public TranslationProvider Provider { get; init; }

    public TranslationModule2<TKey> this[string moduleName]
    {
        get => _modules[moduleName];
        set => _modules[moduleName] = value;
    }

    public bool ContainsModule(string moduleName) => _modules.ContainsKey(moduleName);

    public void AddModule(TranslationModule2<TKey> module) => _modules.Add(module.Name, module);

    public IReadOnlyCollection<TranslationModule2<TKey>> Modules => _modules.Values;
}

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

public record TranslationTable(string Namespace, string ClassName, string Name);
