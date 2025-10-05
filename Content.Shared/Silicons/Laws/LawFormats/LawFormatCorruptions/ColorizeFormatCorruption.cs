using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Silicons.Laws.LawFormats.LawFormatCorruptions;

/// <summary>
/// Applies one of the specified colors at random to the text of the law.
/// Uses uniform distribution to select the color to apply.
/// </summary>
public sealed partial class ColorizeFormatCorruption : LawFormatCorruption
{
    [DataField]
    public List<ProtoId<LawFormatPrototype>>? PossibleColors = [];

    public override ProtoId<LawFormatPrototype>? FormatToApply()
    {
        if (PossibleColors is null || PossibleColors.Count == 0)
            return null;

        if (PossibleColors.Count == 1)
            return PossibleColors[0];

        var random = IoCManager.Resolve<IRobustRandom>();

        var pickedColor = random.Pick(PossibleColors);
        return pickedColor;
    }
}