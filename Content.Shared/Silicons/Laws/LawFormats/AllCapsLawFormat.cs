namespace Content.Shared.Silicons.Laws.LawFormats;

/// <summary>
/// Turns the law string into ALL CAPITAL LETTERS.
/// </summary>
public sealed partial class AllCapsLawFormat : LawFormat
{
    public override string ApplyFormat(string toFormat)
    {
        return Loc.GetString(toFormat).ToUpperInvariant();
    }
}
