using Robust.Shared.GameStates;

namespace Content.Shared.Ghost.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhostOnMoveComponent : Component
{
    [DataField]
    public bool CanReturn = true;

    [DataField]
    public bool MustBeDead;
}
