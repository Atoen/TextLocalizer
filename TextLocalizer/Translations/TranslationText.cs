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

public record AggregatedTranslationData
{
    public readonly string Language; // English, Polish, etc
    public readonly bool IsDefault; // Default is used as fallback if other language is missing some values
    public readonly Dictionary<string, TranslationModule> Modules = []; // Main, UserStatus, some other section, etc
    public readonly ProviderData ProviderData;

    public AggregatedTranslationData(string language, bool isDefault, ProviderData providerData)
    {
        Language = language;
        IsDefault = isDefault;
        ProviderData = providerData;
        // Modules = modules;
    }
}

public record TranslationModule
{
    public readonly string Name; // Main, UserStatus, Settings, etc
    public readonly string SourceFilePath;
    public readonly Dictionary<string, TranslationText> Texts = [];

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
}
