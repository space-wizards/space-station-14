using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed class FloorOcclusionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("enabled")]
    public bool Enabled;

    [ViewVariables, DataField("colliding")]
    public readonly List<EntityUid> Colliding = new();
}
