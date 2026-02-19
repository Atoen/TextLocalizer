namespace TextLocalizer.Types;

public interface ILocalizedTextProvider
{
    string? this[StringResourceId key] { get; }
}
