using Content.Shared.Light.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This handle randomly destroying lights, causing them to flicker endlessly, or replacing their tube/bulb with different variants.
/// </summary>
[RegisterComponent]
public sealed partial class PoweredLightVariationPassComponent : Component
{
    /// <summary>
    ///     Chance that a light will be replaced with a broken variant.
    /// </summary>
    [DataField]
    public float LightBreakChance = 0.15f;

    /// <summary>
    ///     Chance that a light will be replaced with an aged variant.
    /// </summary>
    [DataField]
    public float LightAgingChance = 0.05f;

    [DataField]
    public float AgedLightTubeFlickerChance = 0.00f; //imp edit; was 0.03f / 3%
    //note this doesn't remove Aged Light Tube spawning from the game, it just removes the chance that they endlessly flicker!

    [DataField]
    public EntProtoId BrokenLightBulbPrototype = "LightBulbBroken";

    [DataField]
    public EntProtoId BrokenLightTubePrototype = "LightTubeBroken";

    [DataField]
    public EntProtoId AgedLightBulbPrototype = "LightBulbOld";

    [DataField]
    public EntProtoId AgedLightTubePrototype = "LightTubeOld";
}
