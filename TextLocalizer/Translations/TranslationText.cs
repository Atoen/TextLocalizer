namespace TextLocalizer.Translations;

public record ProviderData
{
    public bool IsDefault { get; }
    public readonly string Namespace;
    public readonly string ClassName;

    public ProviderData(string @namespace, string className, bool isDefault)
    {
        IsDefault = isDefault;
        Namespace = @namespace;
        ClassName = className;
    }
}

public record TableData
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly string Name;

    public TableData(string @namespace, string className, string name)
    {
        Namespace = @namespace;
        ClassName = className;
        Name = name;
    }
}

internal record Data2<TKey>
{
    public readonly TableData TableData;
    public readonly Dictionary<string, AggregatedTranslationData<TKey>> Translations;
    public readonly GeneratorSettings GeneratorSettings;

    public Data2(TableData tableData, Dictionary<string, AggregatedTranslationData<TKey>> translations, GeneratorSettings generatorSettings)
    {
        TableData = tableData;
        Translations = translations;
        GeneratorSettings = generatorSettings;
    }
}

public record AggregatedTranslationData<TKey>
{
    public readonly string Language; // English, Polish, etc
    public readonly bool IsDefault; // Default is used as fallback if other language is missing some values
    public readonly Dictionary<string, TranslationModule<TKey>> Modules = []; // Main, UserStatus, some other section, etc
    public readonly ProviderData ProviderData;

    public AggregatedTranslationData(string language, bool isDefault, ProviderData providerData)
    {
        Language = language;
        IsDefault = isDefault;
        ProviderData = providerData;
        // Modules = modules;
    }

    public AggregatedTranslationData<T> ToKey<T>()
    {
        return new AggregatedTranslationData<T>(Language, IsDefault, ProviderData);
    }
}

public record TranslationModule<TKey>
{
    public readonly string Name; // Main, UserStatus, Settings, etc
    public readonly string SourceFilePath;
    public readonly Dictionary<TKey, TranslationText> Texts = [];

    public TranslationModule(string name, string sourceFilePath)
    {
        Name = name;
        SourceFilePath = sourceFilePath;
        // Texts = texts;
    }
}

public record TranslationText
{
    public readonly string Key;
    public readonly string Value;
    public readonly string? Description;
    public readonly int SourceFileLineNumber;
    public readonly bool IsUntranslatable;
    public readonly bool IsTemplated;

    public TranslationText(string key, string value, string? description, int sourceFileLineNumber, bool isUntranslatable, bool isTemplated)
    {
        Key = key;
        Value = value;
        Description = description;
        SourceFileLineNumber = sourceFileLineNumber;
        IsUntranslatable = isUntranslatable;
        IsTemplated = isTemplated;
    }

    // public TranslationText<T> WithKey<T>(T key)
    // {
    //     return new TranslationText<T>(key, Value, Description, SourceFileLineNumber, IsUntranslatable, IsTemplated);
    // }
}
