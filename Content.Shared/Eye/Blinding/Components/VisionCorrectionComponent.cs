using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     This component allows equipment to offset blurry vision.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VisionCorrectionComponent : Component
{
    /// <summary>
    /// Amount of effective eye damage to add when this item is worn
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("visionBonus"), AutoNetworkedField]
    public float VisionBonus = 0f;

    /// <summary>
    /// Controls the exponent of the blur effect when worn
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("correctionPower"), AutoNetworkedField]
    public float CorrectionPower = 2f;
}
