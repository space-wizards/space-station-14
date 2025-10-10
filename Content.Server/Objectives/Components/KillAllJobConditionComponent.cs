using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that all players of a specified job are eliminated (or marooned if configured).
/// Works similarly to KillPersonCondition but aggregates across all minds with the target job.
/// </summary>
[RegisterComponent, Access(typeof(KillAllJobConditionSystem))]
public sealed partial class KillAllJobConditionComponent : Component
{
    /// <summary>
    /// Target job to eliminate.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    /// <summary>
    /// Whether each target must be dead. If false, being marooned off the evac shuttle counts.
    /// </summary>
    [DataField]
    public bool RequireDead = true;

    /// <summary>
    /// Whether each target must not be on evac (i.e., marooned). If emergency shuttle is disabled, this falls back to RequireDead.
    /// </summary>
    [DataField]
    public bool RequireMaroon = false;
}
