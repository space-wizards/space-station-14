using System.Linq;
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
    public (EntityUid, IReadOnlyList<EntityUid>, IReadOnlyList<GridId>) LoadGameMap(GameMapPrototype map, MapId targetMapId, MapLoadOptions? loadOptions, string? stationName = null, bool addToPriorList = false)
    {
        var loadOpts = loadOptions ?? new MapLoadOptions();

        var ev = new PreLoadGameMapEvent(targetMapId, map, loadOpts);
        RaiseLocalEvent(ev);

        var (entities, gridIds) = _mapLoader.LoadMap(targetMapId, ev.GameMap.MapPath.ToString(), ev.Options);

        var bookkeeper = StartBookkeepingMap(map, gridIds);
        if (addToPriorList)
            _previousMaps.Add(map.ID);

        RaiseLocalEvent(new PostLoadGameMapEvent(map, targetMapId, entities, gridIds, stationName, bookkeeper));

        return (bookkeeper, entities, gridIds);
    }

    /// <summary>
    /// Loads in all maps with the given settings.
    /// </summary>
    /// <remarks>
    /// This is not designed to load into existing maps, and is meant for creating a new play environment (typically, at round-start)
    /// </remarks>
    public List<MapId> LoadGameMaps(List<(GameMapPrototype proto, MapLoadOptions options, int mapIdx)> toLoad, bool makeMapsUninitialized, bool addToPriorList)
    {
        var ev = new PreLoadGameMapsEvent(toLoad, makeMapsUninitialized, addToPriorList);
        // Allow listeners to modify the toLoad list, or outright handle the event.
        RaiseLocalEvent(ev);

        if (ev.Handled && ev.WorldMaps != null)
            return ev.WorldMaps; // Some other system loaded maps for us.

        var worldMaps = new Dictionary<int, MapId>();

        foreach (var (_, _, mapIdx) in toLoad)
        {
            if (worldMaps.ContainsKey(mapIdx))
                continue;

            worldMaps.Add(mapIdx, _mapManager.CreateMap());

            if (makeMapsUninitialized)
                _mapManager.AddUninitializedMap(worldMaps[mapIdx]);
        }

        var loadedDict = new Dictionary<EntityUid, (IReadOnlyList<EntityUid> Entities, IReadOnlyList<GridId> Grids)>();

        foreach (var (proto, options, mapIdx) in toLoad)
        {
            var (bookkeeper, entities, grids) = LoadGameMap(proto, worldMaps[mapIdx], options, null, addToPriorList);
            loadedDict.Add(bookkeeper, (entities, grids));
        }

        var worldMapList = worldMaps.Values.ToList();

        RaiseLocalEvent(new PostLoadGameMapsEvent(
            toLoad,
            worldMapList,
            loadedDict,
            makeMapsUninitialized
            ));

        return worldMapList;
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
public sealed class PreLoadGameMapEvent : EntityEventArgs
{
    public readonly MapId Map;
    public GameMapPrototype GameMap;
    public MapLoadOptions Options;

    public PreLoadGameMapEvent(MapId map, GameMapPrototype gameMap, MapLoadOptions options)
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
public sealed class PostLoadGameMapEvent : EntityEventArgs
{
    public readonly GameMapPrototype GameMap;
    public readonly MapId Map;
    public readonly IReadOnlyList<EntityUid> Entities;
    public readonly IReadOnlyList<GridId> Grids;
    public readonly string? StationName;
    public readonly EntityUid Bookkeeper;

    public PostLoadGameMapEvent(GameMapPrototype gameMap, MapId map, IReadOnlyList<EntityUid> entities, IReadOnlyList<GridId> grids, string? stationName, EntityUid bookkeeper)
    {
        GameMap = gameMap;
        Map = map;
        Entities = entities;
        Grids = grids;
        StationName = stationName;
        Bookkeeper = bookkeeper;
    }
}

/// <summary>
/// Ordered broadcast event raised before maps are loaded.
/// Contains a list of game map prototypes to load; modify it if you want to load different maps,
/// for example as part of a game rule or to replace the default map loader.
/// </summary>
/// <remarks>
/// It's expected that systems that handle this event will fire PostLoadGameMapsEvent, even if they have a replacement for it.
/// This is to make sure that code not adapted for the replacement will continue to function (unless that's undesired)
/// </remarks>
[PublicAPI]
public sealed class PreLoadGameMapsEvent : HandledEntityEventArgs
{
    /// <summary>
    /// Maps to be loaded.
    /// </summary>
    public List<(GameMapPrototype proto, MapLoadOptions options, int mapIdx)> Maps;
    /// <summary>
    /// The list of created world maps, if any, added by a handling system.
    /// </summary>
    public List<MapId>? WorldMaps;
    /// <summary>
    /// Whether or not the maps will be uninitialized.
    /// </summary>
    public readonly bool MakeMapsUninitialized;
    /// <summary>
    /// Whether or not the maps being loaded will be logged.
    /// </summary>
    public readonly bool AddToPriorList;

    public PreLoadGameMapsEvent(List<(GameMapPrototype proto, MapLoadOptions options, int mapIdx)> maps, bool makeMapsUninitialized, bool addToPriorList)
    {
        Maps = maps;
        MakeMapsUninitialized = makeMapsUninitialized;
        AddToPriorList = addToPriorList;
    }
}

/// <summary>
/// Broadcast event raised after maps are loaded.
/// </summary>
[PublicAPI]
public sealed class PostLoadGameMapsEvent : EntityEventArgs
{
    /// <summary>
    /// The configuration used to load the maps.
    /// </summary>
    public readonly IReadOnlyList<(GameMapPrototype proto, MapLoadOptions options, int mapIdx)> MapConfiguration;
    /// <summary>
    /// The list of created world maps.
    /// </summary>
    public readonly IReadOnlyList<MapId> WorldMaps;
    /// <summary>
    /// The index of maps, indexed by bookkeeper.
    /// </summary>
    public readonly IReadOnlyDictionary<EntityUid, (IReadOnlyList<EntityUid> Entities, IReadOnlyList<GridId> Grids)> LoadedMaps;
    /// <summary>
    /// Whether or not the maps were loaded uninitialized.
    /// </summary>
    public readonly bool Uninitialized;

    public PostLoadGameMapsEvent(IReadOnlyList<(GameMapPrototype proto, MapLoadOptions options, int mapIdx)> mapConfiguration, IReadOnlyList<MapId> worldMaps, IReadOnlyDictionary<EntityUid, (IReadOnlyList<EntityUid> Entities, IReadOnlyList<GridId> Grids)> loadedMaps, bool uninitialized)
    {
        MapConfiguration = mapConfiguration;
        WorldMaps = worldMaps;
        LoadedMaps = loadedMaps;
        Uninitialized = uninitialized;
    }
}
