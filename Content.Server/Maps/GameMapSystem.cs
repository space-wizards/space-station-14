using Content.Server.GameTicking;
using Content.Server.Voting.Managers;
using Content.Shared.Coordinates;
using JetBrains.Annotations;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Maps;

/// <summary>
/// Manages map loading and map rotation.
/// </summary>
public sealed partial class GameMapSystem : EntitySystem
{

    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        InitializeCVars();
        InitializeBookkeeping();
        InitializeMapSelection();
    }

    /// <summary>
    /// Loads a new map, allowing systems interested in it to handle loading events.
    /// In the base game, this is required to be used if you want to load a station.
    /// </summary>
    /// <param name="map">Game map prototype to load in.</param>
    /// <param name="targetMapId">Map to load into.</param>
    /// <param name="loadOptions">Map loading options, includes offset.</param>
    /// <param name="stationName">Name to assign to the loaded station.</param>
    /// <param name="addToPriorList">Whether or not to add this map to PreviousMaps</param>
    /// <returns>All loaded entities and grids.</returns>
    public (IReadOnlyList<EntityUid>, IReadOnlyList<GridId>) LoadGameMap(GameMapPrototype map, MapId targetMapId, MapLoadOptions? loadOptions, string? stationName = null, bool addToPriorList = false)
    {
        var loadOpts = loadOptions ?? new MapLoadOptions();

        var ev = new PreGameMapLoad(targetMapId, map, loadOpts);
        RaiseLocalEvent(ev);

        var (entities, gridIds) = _mapLoader.LoadMap(targetMapId, ev.GameMap.MapPath.ToString(), ev.Options);

        var bookkeeper = StartBookkeepingMap(map, gridIds);
        if (addToPriorList)
            _previousMaps.Add(map.ID);

        RaiseLocalEvent(new PostGameMapLoad(map, targetMapId, entities, gridIds, stationName, bookkeeper));

        return (entities, gridIds);
    }
}

/// <summary>
/// Ordered broadcast event raised before the game loads a given map.
/// This event is mutable, and load options should be tweaked if necessary.
/// </summary>
/// <remarks>
/// You likely want to subscribe to this after StationSystem.
/// </remarks>
[PublicAPI]
public sealed class PreGameMapLoad : EntityEventArgs
{
    public readonly MapId Map;
    public GameMapPrototype GameMap;
    public MapLoadOptions Options;

    public PreGameMapLoad(MapId map, GameMapPrototype gameMap, MapLoadOptions options)
    {
        Map = map;
        GameMap = gameMap;
        Options = options;
    }
}

/// <summary>
/// Ordered broadcast event raised after the game loads a given map.
/// </summary>
/// <remarks>
/// You likely want to subscribe to this after StationSystem.
/// </remarks>
[PublicAPI]
public sealed class PostGameMapLoad : EntityEventArgs
{
    public readonly GameMapPrototype GameMap;
    public readonly MapId Map;
    public readonly IReadOnlyList<EntityUid> Entities;
    public readonly IReadOnlyList<GridId> Grids;
    public readonly string? StationName;
    public readonly EntityUid Bookkeeper;

    public PostGameMapLoad(GameMapPrototype gameMap, MapId map, IReadOnlyList<EntityUid> entities, IReadOnlyList<GridId> grids, string? stationName, EntityUid bookkeeper)
    {
        GameMap = gameMap;
        Map = map;
        Entities = entities;
        Grids = grids;
        StationName = stationName;
        Bookkeeper = bookkeeper;
    }
}
