namespace Content.Server.Silicons.Laws.LawFormatCorruptions;

/// <summary>
/// Applies no flavor formatting.
/// Exists to allow no formatting to be selected as a weighted random option.
/// Also for type safety.
/// </summary>
public sealed partial class NoFormatCorruption : LawFormatCorruption
{
    public override string? ApplyFormatCorruption(string toFormat)
    {
        return null;
    }
}