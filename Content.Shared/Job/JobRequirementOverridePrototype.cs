using Content.Shared.Antag;
using Content.Shared.Ghost.Roles;
using Content.Shared.Job;
using Content.Shared.Roles.Requirements;
using Robust.Shared.Prototypes;

namespace Content.Shared.Job;

/// <summary>
/// Collection of job, antag, and ghost-role job requirements for per-server requirement overrides.
/// </summary>
[Prototype]
public sealed partial class JobRequirementOverridePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, HashSet<RoleRequirement>> Jobs = new ();

    [DataField]
    public Dictionary<ProtoId<AntagPrototype>, HashSet<RoleRequirement>> Antags = new ();

    [DataField]
    public Dictionary<ProtoId<GhostRolePrototype>, HashSet<RoleRequirement>> GhostRoles = new();
}
