using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Components;

/// <summary>
/// Controls spawning on the given station, tracking spawners present on it.
/// </summary>
[RegisterComponent, Access(typeof(StationSpawningSystem), typeof(SpawnPointSystem))]
public sealed partial class StationSpawningComponent : Component
{
    /// <summary>
    /// List of possible spawnpoints for each job.
    /// </summary>
    public Dictionary<ProtoId<JobPrototype>, List<EntityCoordinates>> JobSpawnPoints = new();

    /// <summary>
    /// List of possible latejoin spawnpoints on the station.
    /// </summary>
    public List<EntityCoordinates> LateJoinSpawnPoints = new();
}
