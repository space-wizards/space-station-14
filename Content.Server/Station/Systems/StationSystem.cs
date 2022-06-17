using System.Linq;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Station.Systems;

/// <summary>
/// System that manages stations.
/// A station is, by default, just a name, optional map prototype, and optional grids.
/// For jobs, look at StationJobSystem. For spawning, look at StationSpawningSystem.
/// </summary>
[PublicAPI]
public sealed class StationSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private ISawmill _sawmill = default!;

    private readonly HashSet<EntityUid> _stations = new();

    /// <summary>
    /// All stations that currently exist.
    /// </summary>
    /// <remarks>
    /// I'd have this just invoke an entity query, but I want this to be a hashset for convenience and it allocating on use would be lame.
    /// </remarks>
    public IReadOnlySet<EntityUid> Stations => _stations;

    private bool _randomStationOffset;
    private bool _randomStationRotation;
    private float _maxRandomStationOffset;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("station");

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
        SubscribeLocalEvent<PreGameMapLoad>(OnPreGameMapLoad);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<StationDataComponent, ComponentAdd>(OnStationStartup);
        SubscribeLocalEvent<StationDataComponent, ComponentShutdown>(OnStationDeleted);

        _configurationManager.OnValueChanged(CCVars.StationOffset, x => _randomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.MaxStationOffset, x => _maxRandomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.StationRotation, x => _randomStationRotation = x, true);
    }

    #region Event handlers

    private void OnStationStartup(EntityUid uid, StationDataComponent component, ComponentAdd args)
    {
        _stations.Add(uid);
    }

    private void OnStationDeleted(EntityUid uid, StationDataComponent component, ComponentShutdown args)
    {
        _stations.Remove(uid);
    }

    private void OnPreGameMapLoad(PreGameMapLoad ev)
    {
        // this is only for maps loaded during round setup!
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
            return;

        if (_randomStationOffset)
            ev.Options.Offset += _random.NextVector2(_maxRandomStationOffset);

        if (_randomStationRotation)
            ev.Options.Rotation = _random.NextAngle();
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        var dict = new Dictionary<string, List<EntityUid>>();

        void AddGrid(string station, EntityUid grid)
        {
            if (dict.ContainsKey(station))
            {
                dict[station].Add(grid);
            }
            else
            {
                dict[station] = new List<EntityUid> {grid};
            }
        }

        // Iterate over all BecomesStation
        foreach (var grid in ev.Grids)
        {
            // We still setup the grid
            if (!TryComp<BecomesStationComponent>(grid, out var becomesStation))
                continue;

            AddGrid(becomesStation.Id, grid);
        }

        if (!dict.Any())
        {
            // Oh jeez, no stations got loaded.
            // We'll just take the first grid and setup that, then.

            var grid = ev.Grids[0];

            AddGrid("Station", grid);
        }

        // Iterate over all PartOfStation
        foreach (var grid in ev.Grids)
        {
            if (!TryComp<PartOfStationComponent>(grid, out var partOfStation))
                continue;

            AddGrid(partOfStation.Id, grid);
        }

        foreach (var (id, gridIds) in dict)
        {
            StationConfig? stationConfig = null;
            if (ev.GameMap.Stations.ContainsKey(id))
                stationConfig = ev.GameMap.Stations[id];
            else
                _sawmill.Error($"The station {id} in map {ev.GameMap.ID} does not have an associated station config!");
            InitializeNewStation(stationConfig, gridIds, ev.StationName);
        }
    }

    private void OnRoundEnd(GameRunLevelChangedEvent eventArgs)
    {
        if (eventArgs.New != GameRunLevel.PreRoundLobby) return;

        foreach (var entity in _stations)
        {
            Del(entity);
        }
    }

    #endregion Event handlers


    /// <summary>
    /// Generates a station name from the given config.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static string GenerateStationName(StationConfig config)
    {
        return config.NameGenerator is not null
            ? config.NameGenerator.FormatName(config.StationNameTemplate)
            : config.StationNameTemplate;
    }

    /// <summary>
    /// Initializes a new station with the given information.
    /// </summary>
    /// <param name="stationConfig">The game map prototype used, if any.</param>
    /// <param name="gridIds">All grids that should be added to the station.</param>
    /// <param name="name">Optional override for the station name.</param>
    /// <returns>The initialized station.</returns>
    public EntityUid InitializeNewStation(StationConfig? stationConfig, IEnumerable<EntityUid>? gridIds, string? name = null)
    {
        //HACK: This needs to go in null-space but that crashes currently.
        var station = Spawn(null, new MapCoordinates(0, 0, _gameTicker.DefaultMap));
        var data = AddComp<StationDataComponent>(station);
        var metaData = MetaData(station);
        data.StationConfig = stationConfig;

        if (stationConfig is not null && name is null)
        {
            name = GenerateStationName(stationConfig);
        }
        else if (name is null)
        {
            _sawmill.Error($"When setting up station {station}, was unable to find a valid name in the config and no name was provided.");
            name = "unnamed station";
        }

        metaData.EntityName = name;
        RaiseLocalEvent(new StationInitializedEvent(station));
        _sawmill.Info($"Set up station {metaData.EntityName} ({station}).");

        foreach (var grid in gridIds ?? Array.Empty<EntityUid>())
        {
            AddGridToStation(station, grid, null, data, name);
        }

        return station;
    }

    /// <summary>
    /// Adds the given grid to a station.
    /// </summary>
    /// <param name="mapGrid">Grid to attach.</param>
    /// <param name="station">Station to attach the grid to.</param>
    /// <param name="gridComponent">Resolve pattern, grid component of mapGrid.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <exception cref="ArgumentException">Thrown when mapGrid or station are not a grid or station, respectively.</exception>
    public void AddGridToStation(EntityUid station, EntityUid mapGrid, IMapGridComponent? gridComponent = null, StationDataComponent? stationData = null, string? name = null)
    {
        if (!Resolve(mapGrid, ref gridComponent))
            throw new ArgumentException("Tried to initialize a station on a non-grid entity!", nameof(mapGrid));
        if (!Resolve(station, ref stationData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        if (!string.IsNullOrEmpty(name))
            MetaData(mapGrid).EntityName = name;

        var stationMember = AddComp<StationMemberComponent>(mapGrid);
        stationMember.Station = station;
        stationData.Grids.Add(gridComponent.Owner);

        RaiseLocalEvent(station, new StationGridAddedEvent(gridComponent.Owner, false));

        _sawmill.Info($"Adding grid {mapGrid}:{gridComponent.Owner} to station {Name(station)} ({station})");
    }

    /// <summary>
    /// Removes the given grid from a station.
    /// </summary>
    /// <param name="station">Station to remove the grid from.</param>
    /// <param name="mapGrid">Grid to remove</param>
    /// <param name="gridComponent">Resolve pattern, grid component of mapGrid.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <exception cref="ArgumentException">Thrown when mapGrid or station are not a grid or station, respectively.</exception>
    public void RemoveGridFromStation(EntityUid station, EntityUid mapGrid, IMapGridComponent? gridComponent = null, StationDataComponent? stationData = null)
    {
        if (!Resolve(mapGrid, ref gridComponent))
            throw new ArgumentException("Tried to initialize a station on a non-grid entity!", nameof(mapGrid));
        if (!Resolve(station, ref stationData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        RemComp<StationMemberComponent>(mapGrid);
        stationData.Grids.Remove(gridComponent.Owner);

        RaiseLocalEvent(station, new StationGridRemovedEvent(gridComponent.Owner));
        _sawmill.Info($"Removing grid {mapGrid}:{gridComponent.Owner} from station {Name(station)} ({station})");
    }

    /// <summary>
    /// Renames the given station.
    /// </summary>
    /// <param name="station">Station to rename.</param>
    /// <param name="name">The new name to apply.</param>
    /// <param name="loud">Whether or not to announce the rename.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <param name="metaData">Resolve pattern, metadata component of station.</param>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public void RenameStation(EntityUid station, string name, bool loud = true, StationDataComponent? stationData = null, MetaDataComponent? metaData = null)
    {
        if (!Resolve(station, ref stationData, ref metaData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var oldName = metaData.EntityName;
        metaData.EntityName = name;

        if (loud)
        {
            _chatSystem.DispatchStationAnnouncement(station, $"The station {oldName} has been renamed to {name}.");
        }

        RaiseLocalEvent(station, new StationRenamedEvent(oldName, name));
    }

    /// <summary>
    /// Deletes the given station.
    /// </summary>
    /// <param name="station">Station to delete.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public void DeleteStation(EntityUid station, StationDataComponent? stationData = null)
    {
        if (!Resolve(station, ref stationData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        _stations.Remove(station);
        Del(station);
    }

    /// <summary>
    /// Gets the station that "owns" the given entity (essentially, the station the grid it's on is attached to)
    /// </summary>
    /// <param name="entity">Entity to find the owner of.</param>
    /// <param name="xform">Resolve pattern, transform of the entity.</param>
    /// <returns>The owning station, if any.</returns>
    /// <remarks>
    /// This does not remember what station an entity started on, it simply checks where it is currently located.
    /// </remarks>
    public EntityUid? GetOwningStation(EntityUid entity, TransformComponent? xform = null)
    {
        if (!Resolve(entity, ref xform))
            throw new ArgumentException("Tried to use an abstract entity!", nameof(entity));

        if (TryComp<StationDataComponent>(entity, out _))
        {
            // We are the station, just return ourselves.
            return entity;
        }

        if (TryComp<IMapGridComponent>(entity, out _))
        {
            // We are the station, just check ourselves.
            return CompOrNull<StationMemberComponent>(entity)?.Station;
        }

        if (xform.GridEntityId == EntityUid.Invalid)
        {
            Logger.Debug("A");
            return null;
        }

        return CompOrNull<StationMemberComponent>(xform.GridEntityId)?.Station;
    }
}

/// <summary>
/// Broadcast event fired when a station is first set up.
/// This is the ideal point to add components to it.
/// </summary>
[PublicAPI]
public sealed class StationInitializedEvent : EntityEventArgs
{
    /// <summary>
    /// Station this event is for.
    /// </summary>
    public EntityUid Station;

    public StationInitializedEvent(EntityUid station)
    {
        Station = station;
    }
}

/// <summary>
/// Directed event fired on a station when a grid becomes a member of the station.
/// </summary>
[PublicAPI]
public sealed class StationGridAddedEvent : EntityEventArgs
{
    /// <summary>
    /// ID of the grid added to the station.
    /// </summary>
    public EntityUid GridId;

    /// <summary>
    /// Indicates that the event was fired during station setup,
    /// so that it can be ignored if StationInitializedEvent was already handled.
    /// </summary>
    public bool IsSetup;

    public StationGridAddedEvent(EntityUid gridId, bool isSetup)
    {
        GridId = gridId;
        IsSetup = isSetup;
    }
}

/// <summary>
/// Directed event fired on a station when a grid is no longer a member of the station.
/// </summary>
[PublicAPI]
public sealed class StationGridRemovedEvent : EntityEventArgs
{
    /// <summary>
    /// ID of the grid removed from the station.
    /// </summary>
    public EntityUid GridId;

    public StationGridRemovedEvent(EntityUid gridId)
    {
        GridId = gridId;
    }
}

/// <summary>
/// Directed event fired on a station when it is renamed.
/// </summary>
[PublicAPI]
public sealed class StationRenamedEvent : EntityEventArgs
{
    /// <summary>
    /// Prior name of the station.
    /// </summary>
    public string OldName;

    /// <summary>
    /// New name of the station.
    /// </summary>
    public string NewName;

    public StationRenamedEvent(string oldName, string newName)
    {
        OldName = oldName;
        NewName = newName;
    }
}

