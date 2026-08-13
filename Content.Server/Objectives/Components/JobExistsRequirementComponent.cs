using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that at least one of a list of jobs have been taken on the station.
/// </summary>
[RegisterComponent, Access(typeof(JobExistsRequirementSystem))]
public sealed partial class JobExistsRequirementComponent : Component
{
    /// <summary>
    /// List of jobs to check for. Requirement passes if any jobs match.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> Jobs = new();
}
