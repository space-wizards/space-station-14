namespace Content.Shared.Silicons.Laws.LawFormats;

/// <summary>
/// Renders text of the law in the specified color.
/// </summary>
public sealed partial class ColoredLawFormat : LawFormat
{
    [DataField]
    public Color Color;

    public override string ApplyFormat(string toFormat)
    {
        return $"[color={Color.ToHexNoAlpha()}]{Loc.GetString(toFormat)}[/color]";
    }
}
