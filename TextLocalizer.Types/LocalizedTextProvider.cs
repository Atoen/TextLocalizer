namespace TextLocalizer.Types;

public abstract class LocalizedTextProvider
{
    public abstract object? this[int key] { get; }

    public abstract string? Get(int key);
    public abstract string? Get(int key, int count);
    
    public abstract bool IsDefault { get; }

    public virtual PluralCategory GetPluralCategory(int count)
    {
        var abs = Math.Abs(count);
        return abs == 1 ? PluralCategory.One : PluralCategory.Other;
    }
}
