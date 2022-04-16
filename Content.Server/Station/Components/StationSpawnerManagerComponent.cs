using Content.Server.Station.Systems;
using Robust.Shared.Map;

namespace Content.Server.Station.Components;

/// <summary>
/// Bookkeeping component that handles adding and removing spawners from stations.
/// Anything that handles player spawning should have this component, otherwise bookkeeping won't work correctly!
/// </summary>
[RegisterComponent, Friend(typeof(StationSpawningSystem))]
public sealed class StationSpawnerManagerComponent : Component
{
    public GridId? PreviousGrid;
    public EntityUid? PreviousStation;
}
