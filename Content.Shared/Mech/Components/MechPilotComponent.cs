using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MechPilotComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Mech;
}
