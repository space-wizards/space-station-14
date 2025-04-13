using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target makes it to CentComm alive, but not necessarily unrestrained.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(EscortTargetConditionSystem))]
public sealed partial class EscortTargetConditionComponent : Component
{
}
