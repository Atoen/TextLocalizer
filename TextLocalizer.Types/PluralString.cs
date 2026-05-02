namespace TextLocalizer.Types;

public record PluralString(
    string Other,
    string? Zero = null,
    string? One = null,
    string? Two = null,
    string? Few = null,
    string? Many = null)
{
    public string Get(PluralCategory category)
    {
        return category switch
        {
            PluralCategory.Zero => Zero,
            PluralCategory.One => One,
            PluralCategory.Two => Two,
            PluralCategory.Few => Few,
            PluralCategory.Many => Many,
            _ => Other
        } ?? Other;
    }
}
