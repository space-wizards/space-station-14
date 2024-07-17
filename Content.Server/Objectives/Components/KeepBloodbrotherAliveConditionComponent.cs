using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target stays alive.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(KeepBloodbrotherAliveConditionSystem))]
public sealed partial class KeepBloodbrotherAliveConditionComponent : Component
{
}
