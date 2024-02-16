using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

[RegisterComponent]
public sealed partial class SpawnPointComponent : Component
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("job_id")]
    private string? _jobId;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("spawn_type")]
    public SpawnPointType SpawnType { get; private set; } = SpawnPointType.Unset;

    public JobPrototype? Job => string.IsNullOrEmpty(_jobId) ? null : _prototypeManager.Index<JobPrototype>(_jobId);

    public override string ToString()
    {
        return $"{_jobId} {SpawnType}";
    }
}

public enum SpawnPointType
{
    Unset = 0,
    LateJoin,
    Job,
    Observer,
}
