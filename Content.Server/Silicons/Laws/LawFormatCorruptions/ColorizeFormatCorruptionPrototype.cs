using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Laws.LawFormatCorruptions;

/// <summary>
/// Applies one of the specified colors at random to the text of the law.
/// Uses uniform distribution to select the color to apply.
/// </summary>
[Prototype]
public sealed partial class ColorizeFormatCorruptionPrototype : LawFormatCorruptionPrototype
{
    [DataField]
    public List<Color>? PossibleColors;

    public override string? ApplyFormatCorruption(string toFormat)
    {
        if (PossibleColors is null || PossibleColors.Count == 0)
            return null;

        if (PossibleColors.Count == 1)
            return $"[color={PossibleColors[0].ToHexNoAlpha()}]{Loc.GetString(toFormat)}[/color]";

        var random = IoCManager.Resolve<IRobustRandom>();

        var pickedColor = random.Pick(PossibleColors);
        return $"[color={pickedColor.ToHexNoAlpha()}]{Loc.GetString(toFormat)}[/color]";
    }
}