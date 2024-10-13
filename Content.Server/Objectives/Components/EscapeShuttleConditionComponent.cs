using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is on the emergency shuttle's grid when docking to CentComm.
/// </summary>
[RegisterComponent, Access(typeof(EscapeShuttleConditionSystem))]
public sealed partial class EscapeShuttleConditionComponent : Component
{
}
