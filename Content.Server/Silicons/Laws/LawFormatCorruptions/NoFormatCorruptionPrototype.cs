using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Laws.LawFormatCorruptions;

/// <summary>
/// Applies no flavor formatting.
/// Exists to allow no formatting to be selected as a weighted random option.
/// Also for type safety.
/// </summary>
[Prototype]
public sealed partial class NoFormatCorruptionPrototype : LawFormatCorruptionPrototype
{
    public override string? ApplyFormatCorruption(string toFormat)
    {
        return null;
    }
}