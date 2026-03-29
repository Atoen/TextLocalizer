namespace TextLocalizer.Diagnostics;

public enum TranslationErrorType
{
    KeyMissing = 1,
    InvalidKey,
    ModuleMissing,
    UnexpectedKey,
    TranslatedUntranslatable,
    KeyConflictsWithModule
}
