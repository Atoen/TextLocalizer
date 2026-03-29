namespace TextLocalizer.Translations;

public record Translation<TKey>(string Language, bool IsDefault) where TKey : IEquatable<TKey>
{
    private readonly Dictionary<string, TranslationModule<TKey>> _modules = [];

    public TranslationProvider Provider { get; init; }

    public TranslationModule<TKey> this[string moduleName]
    {
        get => _modules[moduleName];
        set => _modules[moduleName] = value;
    }

    public bool ContainsModule(string moduleName) => _modules.ContainsKey(moduleName);

    public void AddModule(TranslationModule<TKey> module) => _modules.Add(module.Name, module);

    public IReadOnlyCollection<TranslationModule<TKey>> Modules => _modules.Values;
}

public record TranslationTable(string Namespace, string ClassName, string Name, string DefaultProvider, string Provider);
