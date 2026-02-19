using TextLocalizer.Translations;

namespace TextLocalizer;

public class TranslationDictionary : Dictionary<string, IndexedLocalizedText>;

public class TranslationDictionary2 : Dictionary<string, Dictionary<string, TranslationText>>;