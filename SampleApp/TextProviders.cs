using TextLocalizer;
using TextLocalizer.Types;

namespace SampleApp;

[TranslationProvider(Language = "en")]
public partial class EnglishTextProvider
{
    public PluralCategory GetPluralCategory(int count)
    {
        count = Math.Abs(count);
        return count == 1
            ? PluralCategory.One
            : PluralCategory.Other;
    }
}

[TranslationProvider(Language = "pl")]
public partial class PolishTextProvider
{
    public PluralCategory GetPluralCategory(int count)
    {
        count = Math.Abs(count);
        if (count == 1)
        {
            return PluralCategory.One;
        }

        var mod10 = count % 10;
        var mod100 = count % 100;

        if (mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14)
        {
            return PluralCategory.Few;
        }

        return PluralCategory.Many;
    }
}

[TranslationProvider(Language = "de")]
public partial class GermanTextProvider
{
    public PluralCategory GetPluralCategory(int count)
    {
        count = Math.Abs(count);
        return count == 1
            ? PluralCategory.One
            : PluralCategory.Other;
    }
}
