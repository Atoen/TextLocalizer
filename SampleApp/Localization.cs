using TextLocalizer;
using TextLocalizer.Types;

namespace SampleApp;

[LocalizationTable(
    CurrentProviderAccessor = nameof(_provider),
    DefaultProviderAccessor = nameof(_defaultProvider),
    TableName = "R"
)]
public partial class Localization
{
    private readonly Dictionary<SupportedLanguage, ILocalizedTextProvider> _textProviders = new();

    public static SupportedLanguage DefaultLanguage { get; set; } = SupportedLanguage.Polish;

    public SupportedLanguage Language { get; private set; } = DefaultLanguage;

    private readonly ILocalizedTextProvider _defaultProvider;
    private ILocalizedTextProvider _provider;

    public Localization()
    {
        _defaultProvider = GetLanguageProvider(DefaultLanguage);
        _provider = _defaultProvider;
    }

    public void SetLanguage(SupportedLanguage language)
    {
        Language = language;
        _provider = GetLanguageProvider(Language);
    }

    public string StringResource(StringResourceId id) => R[id];

    public string StringResource(StringResourceId id, object? arg0)
    {
        return string.Format(R[id], arg0);
    }

    public string StringResource(StringResourceId id, object? arg0, object? arg1)
    {
        return string.Format(R[id], arg0, arg1);
    }

    public string StringResource(StringResourceId id, params ReadOnlySpan<object?> args)
    {
        return string.Format(R[id], args);
    }

    private static ILocalizedTextProvider CreateProvider(SupportedLanguage language) => language.Tag switch
    {
        SupportedLanguage.EnglishTag => new EnglishTextProvider(),
        SupportedLanguage.PolishTag => new PolishTextProvider(),
        SupportedLanguage.GermanTag => new GermanTextProvider(),
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    private ILocalizedTextProvider GetLanguageProvider(SupportedLanguage language)
    {
        if (_textProviders.TryGetValue(language, out var provider))
        {
            return provider;
        }

        var newProvider = CreateProvider(language);
        _textProviders[language] = newProvider;

        return newProvider;
    }
}
