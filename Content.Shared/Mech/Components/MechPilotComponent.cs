using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Components;

/// <summary>
/// Attached to entities piloting a <see cref="MechComponent"/>
/// </summary>
/// <remarks>
/// Get in the robot, Shinji
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechPilotComponent : Component
{
    /// <summary>
    /// The mech being piloted
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid Mech;
}
