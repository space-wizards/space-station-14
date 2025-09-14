using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Laws.LawFormatCorruptions;

/// <summary>
/// Turns the law string into ALL CAPITAL LETTERS. The OG corrupted laws formatting.
/// </summary>
[Prototype]
public sealed partial class AllCapsFormatCorruptionPrototype : LawFormatCorruptionPrototype
{
    public override string? ApplyFormatCorruption(string toFormat)
    {
        return Loc.GetString(toFormat).ToUpperInvariant();
    }
}