using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server.Station;

/// <summary>
/// System that manages the jobs available on a station, and maybe other things later.
/// </summary>
public class StationSystem : EntitySystem
{
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IGameMapManager _gameMapManager = default!;
    private uint _idCounter = 1;

    private Dictionary<StationId, StationInfoData> _stationInfo = new();
    /// <summary>
    /// List of stations for the current round.
    /// </summary>
    public IReadOnlyDictionary<StationId, StationInfoData> StationInfo => _stationInfo;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
    }

    /// <summary>
    /// Cleans up station info.
    /// </summary>
    private void OnRoundEnd(GameRunLevelChangedEvent eventArgs)
    {
        if (eventArgs.New == GameRunLevel.PreRoundLobby)
            _stationInfo = new();
    }

    public class StationInfoData
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

        Logger.InfoS("stations",
            $"Setting up new {mapPrototype.ID} called {_stationInfo[id].Name} on grid {mapGrid}:{gridComponent.GridIndex}");

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

        Logger.InfoS("stations", $"Adding grid {mapGrid}:{gridComponent.GridIndex} to station {station} named {_stationInfo[station].Name}");
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
