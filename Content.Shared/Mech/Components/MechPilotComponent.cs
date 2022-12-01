using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MechPilotComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Mech;
}

[Serializable, NetSerializable]
public sealed class MechPilotComponentState : ComponentState
{
    public EntityUid Mech;
}
