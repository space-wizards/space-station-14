using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Applies an occlusion shader to this entity if it's colliding with a <see cref="FloorOccluderComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class FloorOcclusionComponent : Component
{
    [ViewVariables]
    public bool Enabled => Colliding.Count > 0;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Colliding = new();
}
