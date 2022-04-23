using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Station;

/// <summary>
/// System that manages the jobs available on a station, and maybe other things later.
/// </summary>
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

    private uint _idCounter = 1;

    private Dictionary<StationId, StationInfoData> _stationInfo = new();

    /// <summary>
    /// List of stations currently loaded.
    /// </summary>
    public IReadOnlyDictionary<StationId, StationInfoData> StationInfo => _stationInfo;

    private bool _randomStationOffset = false;
    private bool _randomStationRotation = false;
    private float _maxRandomStationOffset = 0.0f;

    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("station");

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
        SubscribeLocalEvent<PreGameMapLoad>(OnPreGameMapLoad);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);

        _configurationManager.OnValueChanged(CCVars.StationOffset, x => _randomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.MaxStationOffset, x => _maxRandomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.StationRotation, x => _randomStationRotation = x, true);
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
        var dict = new Dictionary<string, StationId>();

        // Iterate over all BecomesStation
        for (var i = 0; i < ev.Grids.Count; i++)
        {
            var grid = ev.Grids[i];

            // We still setup the grid
            if (!TryComp<BecomesStationComponent>(_mapManager.GetGridEuid(grid), out var becomesStation))
                continue;

            var stationId = InitialSetupStationGrid(grid, ev.GameMap, ev.StationName);

            dict.Add(becomesStation.Id, stationId);
        }

        if (!dict.Any())
        {
            // Oh jeez, no stations got loaded.
            // We'll just take the first grid and setup that, then.

            var grid = ev.Grids[0];
            var stationId = InitialSetupStationGrid(grid, ev.GameMap, ev.StationName);

            dict.Add("Station", stationId);
        }

        // Iterate over all PartOfStation
        for (var i = 0; i < ev.Grids.Count; i++)
        {
            var grid = ev.Grids[i];
            var geid = _mapManager.GetGridEuid(grid);
            if (!TryComp<PartOfStationComponent>(geid, out var partOfStation))
                continue;

            if (dict.TryGetValue(partOfStation.Id, out var stationId))
            {
                AddGridToStation(geid, stationId);
            }
            else
            {
                _sawmill.Error($"Grid {grid} ({geid}) specified that it was part of station {partOfStation.Id} which does not exist");
            }
        }
    }

    /// <summary>
    /// Cleans up station info.
    /// </summary>
    private void OnRoundEnd(GameRunLevelChangedEvent eventArgs)
    {
        if (eventArgs.New == GameRunLevel.PreRoundLobby)
            _stationInfo = new();
    }

    public sealed class StationInfoData
    {
        public string Name;

        /// <summary>
        /// Job list associated with the game map.
        /// </summary>
        public readonly GameMapPrototype MapPrototype;

        /// <summary>
        /// The round job list.
        /// </summary>
        private readonly Dictionary<string, int> _jobList;

        public IReadOnlyDictionary<string, int> JobList => _jobList;

        public StationInfoData(string name, GameMapPrototype mapPrototype, Dictionary<string, int> jobList)
        {
            Name = name;
            MapPrototype = mapPrototype;
            _jobList = jobList;
        }

        public bool TryAssignJob(string jobName)
        {
            if (_jobList.ContainsKey(jobName))
            {
                switch (_jobList[jobName])
                {
                    case > 0:
                        _jobList[jobName]--;
                        return true;
                    case -1:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool AdjustJobAmount(string jobName, int amount)
        {
            DebugTools.Assert(amount >= -1);
            _jobList[jobName] = amount;
            return true;
        }
    }

    /// <summary>
    /// Creates a new station and attaches it to the given grid.
    /// </summary>
    /// <param name="mapGrid">grid to attach to</param>
    /// <param name="mapPrototype">game map prototype of the station</param>
    /// <param name="stationName">name of the station to assign, if not the default</param>
    /// <param name="gridComponent">optional grid component of the grid.</param>
    /// <returns>The ID of the resulting station</returns>
    /// <exception cref="ArgumentException">Thrown when the given entity is not a grid.</exception>
    public StationId InitialSetupStationGrid(EntityUid mapGrid, GameMapPrototype mapPrototype, string? stationName = null, IMapGridComponent? gridComponent = null)
    {
        if (!Resolve(mapGrid, ref gridComponent))
            throw new ArgumentException("Tried to initialize a station on a non-grid entity!");

        var jobListDict = mapPrototype.AvailableJobs.ToDictionary(x => x.Key, x => x.Value[1]);
        var id = AllocateStationInfo();

        _stationInfo[id] = new StationInfoData(stationName ?? _gameMapManager.GenerateMapName(mapPrototype), mapPrototype, jobListDict);
        var station = EntityManager.AddComponent<StationComponent>(mapGrid);
        station.Station = id;

        _gameTicker.UpdateJobsAvailable(); // new station means new jobs, tell any lobby-goers.

        _sawmill.Info($"Setting up new {mapPrototype.ID} called {_stationInfo[id].Name} on grid {mapGrid}:{gridComponent.GridIndex}");

        return id;
    }

    /// <summary>
    /// Adds the given grid to the given station.
    /// </summary>
    /// <param name="mapGrid">grid to attach</param>
    /// <param name="station">station to attach the grid to</param>
    /// <param name="gridComponent">optional grid component of the grid.</param>
    /// <exception cref="ArgumentException">Thrown when the given entity is not a grid.</exception>
    public void AddGridToStation(EntityUid mapGrid, StationId station, IMapGridComponent? gridComponent = null)
    {
        if (!Resolve(mapGrid, ref gridComponent))
            throw new ArgumentException("Tried to initialize a station on a non-grid entity!");
        var stationComponent = EntityManager.AddComponent<StationComponent>(mapGrid);
        stationComponent.Station = station;

        _sawmill.Info( $"Adding grid {mapGrid}:{gridComponent.GridIndex} to station {station} named {_stationInfo[station].Name}");
    }

    /// <summary>
    /// Attempts to assign a job on the given station.
    /// Does NOT inform the gameticker that the job roster has changed.
    /// </summary>
    /// <param name="stationId">station to assign to</param>
    /// <param name="job">name of the job</param>
    /// <returns>assignment success</returns>
    public bool TryAssignJobToStation(StationId stationId, JobPrototype job)
    {
        if (stationId != StationId.Invalid)
            return _stationInfo[stationId].TryAssignJob(job.ID);
        else
            return false;
    }

    /// <summary>
    /// Checks if the given job is available.
    /// </summary>
    /// <param name="stationId">station to check</param>
    /// <param name="job">name of the job</param>
    /// <returns>job availability</returns>
    public bool IsJobAvailableOnStation(StationId stationId, JobPrototype job)
    {
        if (_stationInfo[stationId].JobList.TryGetValue(job.ID, out var amount))
            return amount != 0;

        return false;
    }

    private StationId AllocateStationInfo()
    {
        return new StationId(_idCounter++);
    }

    public bool AdjustJobsAvailableOnStation(StationId stationId, JobPrototype job, int amount)
    {
        var ret = _stationInfo[stationId].AdjustJobAmount(job.ID, amount);
        _gameTicker.UpdateJobsAvailable();
        return ret;
    }

    public void RenameStation(StationId stationId, string name, bool loud = true)
    {
        var oldName = _stationInfo[stationId].Name;
        _stationInfo[stationId].Name = name;
        if (loud)
        {
            _chatManager.DispatchStationAnnouncement($"The station {oldName} has been renamed to {name}.");
        }

        // Make sure lobby gets the memo.
        _gameTicker.UpdateJobsAvailable();
    }
}
