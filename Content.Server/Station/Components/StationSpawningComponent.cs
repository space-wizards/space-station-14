using Content.Server.Station.Systems;
using Robust.Shared.Map;

namespace Content.Server.Station.Components;

[RegisterComponent, Friend(typeof(StationSpawningSystem))]
public sealed class StationSpawningComponent : Component
{
    public HashSet<EntityUid> Spawners = new();
    public Dictionary<GridId, HashSet<EntityUid>> SpawnersByGrid = new();
}
