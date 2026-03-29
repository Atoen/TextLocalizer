namespace TextLocalizer.Diagnostics;

public record struct TranslationError(
    TranslationErrorType Type,
    string Language,
    string Module,
    string Key,
    int Line = 0)
{
    public static TranslationError KeyMissing(string language, string module, string key) =>
        new(TranslationErrorType.KeyMissing, language, module, key);

    public static TranslationError UnexpectedKey(string language, string module, string key, int line) =>
        new(TranslationErrorType.UnexpectedKey, language, module, key, line);

    public static TranslationError Untranslatable(string language, string module, string key, int line) =>
        new(TranslationErrorType.TranslatedUntranslatable, language, module, key, line);

    public static TranslationError ModuleMissing(string language, string module) =>
        new(TranslationErrorType.ModuleMissing, language, module, "");

    public static TranslationError InvalidKey(string language, string module, string key, int line) =>
        new(TranslationErrorType.InvalidKey, language, module, key, line);

    public static TranslationError KeyConflictsWithModule(string language, string module, string key, int line) =>
        new(TranslationErrorType.KeyConflictsWithModule, language, module, key, line);
}
