using Content.Server.Maps;
using Content.Server.Station.Systems;
using Robust.Shared.Map;

namespace Content.Server.Station.Components;


[RegisterComponent, Friend(typeof(StationSystem))]
public sealed class StationDataComponent : Component
{
    /// <summary>
    /// The game map prototype, if any, associated with this station.
    /// </summary>
    [ViewVariables]
    public GameMapPrototype? MapPrototype = null;

    /// <summary>
    /// List of all grids this station is part of.
    /// You shouldn't mutate this.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<GridId> Grids = new();
}
