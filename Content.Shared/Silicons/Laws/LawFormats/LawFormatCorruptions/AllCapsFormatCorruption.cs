using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.LawFormats.LawFormatCorruptions;

/// <summary>
/// Turns the law string into ALL CAPITAL LETTERS. The OG corrupted laws formatting.
/// </summary>
public sealed partial class AllCapsFormatCorruption : LawFormatCorruption
{
    private static readonly ProtoId<LawFormatPrototype> AllCapsLawFormat = "AllCapsFormat";

    public override ProtoId<LawFormatPrototype>? FormatToApply()
    {
        return AllCapsLawFormat;
    }
}