namespace Content.Server.Silicons.Laws.LawFormatCorruptions;

/// <summary>
/// Turns the law string into ALL CAPITAL LETTERS. The OG corrupted laws formatting.
/// </summary>
public sealed partial class AllCapsFormatCorruption : LawFormatCorruption
{
    public override string? ApplyFormatCorruption(string toFormat)
    {
        return Loc.GetString(toFormat).ToUpperInvariant();
    }
}