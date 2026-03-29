namespace TextLocalizer.Types;

public readonly record struct StringResourceId
{
    private readonly int _value;

    public StringResourceId(int value)
    {
        _value = value;
    }

    public static implicit operator int(StringResourceId stringResourceId)
    {
        return stringResourceId._value;
    }

    public static explicit operator StringResourceId(int id)
    {
        return new StringResourceId(id);
    }
}
