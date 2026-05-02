using TextLocalizer;
using TextLocalizer.Types;

namespace SampleApp;

[TranslationProvider(Language = "en")]
public partial class EnglishTextProvider;

[TranslationProvider(Language = "pl")]
public partial class PolishTextProvider
{
    public override PluralCategory GetPluralCategory(int count)
    {
        var abs = Math.Abs(count);
        if (abs == 1)
        {
            return PluralCategory.One;
        }

        var mod10 = abs % 10;
        var mod100 = abs % 100;

        if (mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14)
        {
            return PluralCategory.Few;
        }

        return PluralCategory.Many;
    }
}

[TranslationProvider(Language = "de")]
public partial class GermanTextProvider;
