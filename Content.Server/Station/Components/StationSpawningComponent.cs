using Content.Server.Station.Systems;
using Robust.Shared.Map;

namespace Content.Server.Station.Components;

/// <summary>
/// Controls spawning on the given station, tracking spawners present on it.
/// </summary>
[RegisterComponent, Friend(typeof(StationSpawningSystem))]
public sealed class StationSpawningComponent : Component
{
    /// <summary>
    /// All spawners known to this station.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Spawners = new();
    /// <summary>
    /// Spawners sorted by the grid they're part of.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<GridId, HashSet<EntityUid>> SpawnersByGrid = new();
}
