using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is on the emergency shuttle or an escape pod when docking to CentComm.
/// </summary>
[RegisterComponent, Access(typeof(EscapeConditionSystem))]
public sealed partial class EscapeConditionComponent : Component
{
}
