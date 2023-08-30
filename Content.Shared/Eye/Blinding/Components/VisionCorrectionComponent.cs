using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     This component allows equipment to offset blurry vision.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VisionCorrectionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("visionBonus"), AutoNetworkedField]
    public float VisionBonus = 3f;
}
