namespace SampleApp;

public sealed record SupportedLanguage
{
    public const string EnglishTag = "en";
    public const string PolishTag = "pl";
    public const string GermanTag = "de";

    public readonly static SupportedLanguage English = new(EnglishTag);
    public readonly static SupportedLanguage Polish = new(PolishTag);
    public readonly static SupportedLanguage German = new(GermanTag);

    public string Tag { get; }

    private SupportedLanguage(string tag)
    {
        Tag = tag;
    }

    public static SupportedLanguage? ParseLanguageCode(string languageCode) => languageCode switch
    {
        EnglishTag => English,
        PolishTag => Polish,
        GermanTag => German,
        _ => null
    };
}
