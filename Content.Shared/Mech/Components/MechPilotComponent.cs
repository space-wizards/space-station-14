using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Components;

/// <summary>
/// Attached to entities piloting a <see cref="MechComponent"/>
/// </summary>
/// <remarks>
/// Get in the robot, Shinji
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechPilotComponent : Component
{
    /// <summary>
    /// The mech being piloted
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Mech;
}

[Serializable, NetSerializable]
public sealed class MechPilotComponentState : ComponentState
{
    public NetEntity Mech;
}
