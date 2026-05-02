namespace TextLocalizer.Types;

public interface IFormattableValue
{
    string GetFormatted(StringFormat format);
}