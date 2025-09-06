using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

[RegisterComponent]
public sealed partial class SpawnPointComponent : Component, ISpawnPoint
{
    /// <summary>
    /// The job this spawn point is valid for.
    /// Null will allow all jobs to spawn here.
    /// </summary>
    [DataField("job_id")]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// The type of spawn point.
    /// </summary>
    [DataField("spawn_type"), ViewVariables(VVAccess.ReadWrite)]
    public SpawnPointType SpawnType { get; set; } = SpawnPointType.Unset;

    /// <summary>
    /// Whitelist of entities that can be spawned at this SpawnPoint
    /// </summary>
    [DataField]
    public EntityWhitelist? SpawnWhitelist;

    public override string ToString()
    {
        return $"{Job} {SpawnType}";
    }
}

public enum SpawnPointType
{
    Unset = 0,
    LateJoin,
    Job,
    Observer,
}
