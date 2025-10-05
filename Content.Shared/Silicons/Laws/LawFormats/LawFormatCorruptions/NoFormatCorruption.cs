using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.LawFormats.LawFormatCorruptions;

/// <summary>
/// Applies no flavor formatting.
/// Exists to allow no formatting to be selected as a weighted random option.
/// Also for type safety.
/// </summary>
public sealed partial class NoFormatCorruption : LawFormatCorruption
{
    public override ProtoId<LawFormatPrototype>? FormatToApply()
    {
        return null;
    }
}