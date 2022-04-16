using Content.Server.Station.Systems;
using Robust.Shared.Map;

namespace Content.Server.Station.Components;

[RegisterComponent, Friend(typeof(StationSpawningSystem))]
public sealed class StationSpawningComponent : Component
{
    /// <summary>
    /// All spawners known to this station.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Spawners = new();
    /// <summary>
    /// Spawners sorted by the grid they're part of.
    /// </summary>
    [ViewVariables]
    public Dictionary<GridId, HashSet<EntityUid>> SpawnersByGrid = new();
}
