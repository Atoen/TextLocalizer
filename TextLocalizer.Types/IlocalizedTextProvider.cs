namespace TextLocalizer.Types;

public interface ILocalizedTextProvider
{
    string? this[int key] { get; }
    
    bool IsDefault { get; }

    PluralCategory GetPluralCategory(int count);
}
