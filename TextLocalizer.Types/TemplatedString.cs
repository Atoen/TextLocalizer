namespace TextLocalizer.Types;

public readonly struct TemplatedString
{
    private readonly string _value;

    public TemplatedString(string value)
    {
        _value = value;
    }

    public string Get(object? param) => string.Format(_value, param);
}
