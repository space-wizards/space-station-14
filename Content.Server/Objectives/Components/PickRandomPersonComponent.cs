namespace Content.Server.Objectives.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person.
/// </summary>
[RegisterComponent]
public sealed partial class PickRandomPersonComponent : Component
{
    /// <summary>
    /// List of jobs, that won't be kill objectives
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>?> IgnoredJobs = new();
}
