using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
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
/// For jobs, look at StationJobSystem. For SIDBs (Station IDentification Beacons) look at StationBeaconSystem.
/// </summary>
[PublicAPI]
public sealed class StationSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private ISawmill _sawmill = default!;

    private readonly HashSet<EntityUid> _stations = new();

    /// <summary>
    /// All stations that currently exist.
    /// </summary>
    public IReadOnlySet<EntityUid> Stations => _stations;

    private bool _randomStationOffset;
    private bool _randomStationRotation;
    private float _maxRandomStationOffset;

    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("station");

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
        SubscribeLocalEvent<PreGameMapLoad>(OnPreGameMapLoad);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<StationDataComponent, ComponentShutdown>(OnStationDeleted);

        _configurationManager.OnValueChanged(CCVars.StationOffset, x => _randomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.MaxStationOffset, x => _maxRandomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.StationRotation, x => _randomStationRotation = x, true);
    }

    #region Event handlers

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
        var dict = new Dictionary<string, List<GridId>>();

        void AddGrid(string station, GridId grid)
        {
            if (dict.ContainsKey(station))
            {
                dict[station].Add(grid);
            }
            else
            {
                dict[station] = new List<GridId> {grid};
            }
        }

        // Iterate over all BecomesStation
        foreach (var grid in ev.Grids)
        {
            // We still setup the grid
            if (!TryComp<BecomesStationComponent>(_mapManager.GetGridEuid(grid), out var becomesStation))
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
            if (!TryComp<PartOfStationComponent>(_mapManager.GetGridEuid(grid), out var partOfStation))
                continue;

            AddGrid(partOfStation.Id, grid);
        }

        foreach (var (_, gridIds) in dict)
        {
            InitializeNewStation(ev.GameMap, gridIds, ev.StationName);
        }
    }

    /// <summary>
    /// Cleans up station info.
    /// </summary>
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
    /// Initializes a new station with the given information.
    /// </summary>
    /// <param name="mapPrototype">The game map prototype used, if any.</param>
    /// <param name="gridIds">All grids that should be added to the station.</param>
    /// <param name="name">Optional override for the station name.</param>
    /// <returns></returns>
    public EntityUid InitializeNewStation(GameMapPrototype? mapPrototype, IEnumerable<GridId>? gridIds, string? name = null)
    {
        var station = Spawn(null, EntityCoordinates.Invalid);
        var data = AddComp<StationDataComponent>(station);
        var metaData = MetaData(station);
        data.MapPrototype = mapPrototype;

        if (gridIds is not null)
            data.Grids.UnionWith(gridIds);

        if (mapPrototype is not null && name is null)
        {
            metaData.EntityName = _gameMapManager.GenerateMapName(mapPrototype);
        }
        else if (name is not null)
        {
            metaData.EntityName = name;
        }

        RaiseLocalEvent(new StationInitializedEvent(station));
        _sawmill.Info($"Set up station {metaData.EntityName} ({station}) with prototype {mapPrototype?.ID}");

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
    public void AddGridToStation(EntityUid station, EntityUid mapGrid, IMapGridComponent? gridComponent = null, StationDataComponent? stationData = null)
    {
        if (!Resolve(mapGrid, ref gridComponent))
            throw new ArgumentException("Tried to initialize a station on a non-grid entity!", nameof(mapGrid));
        if (!Resolve(station, ref stationData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var stationMember = AddComp<StationMemberComponent>(mapGrid);
        stationMember.Station = station;
        stationData.Grids.Add(gridComponent.GridIndex);

        RaiseLocalEvent(station, new StationGridAddedEvent(gridComponent.GridIndex, false));

        _sawmill.Info($"Adding grid {mapGrid}:{gridComponent.GridIndex} to station {Name(station)} ({station})");
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
        stationData.Grids.Remove(gridComponent.GridIndex);

        RaiseLocalEvent(station, new StationGridRemovedEvent(gridComponent.GridIndex));
        _sawmill.Info($"Removing grid {mapGrid}:{gridComponent.GridIndex} from station {Name(station)} ({station})");
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
            _chatManager.DispatchStationAnnouncement($"The station {oldName} has been renamed to {name}.");
        }

        RaiseLocalEvent(station, new StationRenamedEvent(oldName, name));

        // Make sure lobby gets the memo.
        _gameTicker.UpdateJobsAvailable();
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

        Del(station);
    }
}

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
/// Event fired when a grid becomes a member of a station.
/// </summary>
[PublicAPI]
public sealed class StationGridAddedEvent : EntityEventArgs
{
    /// <summary>
    /// ID of the grid added to the station.
    /// </summary>
    public GridId GridId;

    /// <summary>
    /// Indicates that the event was fired during station setup,
    /// so that it can be ignored if StationInitializedEvent was already handled.
    /// </summary>
    public bool IsSetup;

    public StationGridAddedEvent(GridId gridId, bool isSetup)
    {
        GridId = gridId;
        IsSetup = isSetup;
    }
}

/// <summary>
/// Event fired when a grid is no longer a member of a station.
/// </summary>
[PublicAPI]
public sealed class StationGridRemovedEvent : EntityEventArgs
{
    /// <summary>
    /// ID of the grid removed from the station.
    /// </summary>
    public GridId GridId;

    public StationGridRemovedEvent(GridId gridId)
    {
        GridId = gridId;
    }
}

/// <summary>
/// Event fired when a station has been renamed.
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

