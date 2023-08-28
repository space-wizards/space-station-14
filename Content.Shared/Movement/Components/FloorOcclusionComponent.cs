using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Applies an occlusion shader to this entity if it's colliding with a <see cref="FloorOccluderComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class FloorOcclusionComponent : Component
{
    /// <summary>
    /// Is the shader currently enabled.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("enabled"), AutoNetworkedField]
    public bool Enabled;

    [DataField("colliding")]
    public List<EntityUid> Colliding = new();
}
