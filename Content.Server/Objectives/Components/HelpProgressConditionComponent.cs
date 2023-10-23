using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target completes half of their objectives.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(HelpProgressConditionSystem))]
public sealed partial class HelpProgressConditionComponent : Component
{
}
