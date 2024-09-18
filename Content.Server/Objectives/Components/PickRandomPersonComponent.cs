using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person.
/// </summary>
[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class PickRandomPersonComponent : Component
{
    /// <summary>
    /// List of jobs, that won't be kill objectives
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<JobPrototype>?> IgnoredJobs = new();
}
