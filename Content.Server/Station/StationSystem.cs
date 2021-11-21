using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Robust.Shared.GameObjects;

namespace Content.Server.Station
{
    /// <summary>
    /// System that manages the jobs available on a station, and maybe other things later.
    /// </summary>
    public class StationSystem : EntitySystem
    {
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
            if (eventArgs.New == GameRunLevel.PostRound)
                _stationInfo = new();
        }

        public class StationInfoData
        {
            public readonly string Name;

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
        }

        public readonly record struct StationId(uint Id)
        {
            public static StationId Invalid => new(0);
        }

        public StationId InitialSetupStationGrid(EntityUid mapGrid, GameMapPrototype mapPrototype)
        {

            var jobListDict = mapPrototype.AvailableJobs.ToDictionary(x => x.Key, x => x.Value[1]);
            var id = AllocateStationInfo();
            _stationInfo[id] = new StationInfoData(mapPrototype.MapName, mapPrototype, jobListDict);
            var station = EntityManager.AddComponent<StationComponent>(mapGrid);
            station.Station = id;
            return id;
        }

        public void AddGridToStation(EntityUid mapGrid, StationId station)
        {
            var stationComponent = EntityManager.AddComponent<StationComponent>(mapGrid);
            stationComponent.Station = station;
        }

        /// <summary>
        /// Attempts to assign a job on the given station.
        /// </summary>
        /// <param name="stationId">station to assign to</param>
        /// <param name="jobName">name of the job</param>
        /// <returns>assignment success</returns>
        public bool TryAssignJobToStation(StationId stationId, string jobName)
        {
            if (stationId != StationId.Invalid)
                return _stationInfo[stationId].TryAssignJob(jobName);
            else
                return false;
        }

        /// <summary>
        /// Checks if the given job is available.
        /// </summary>
        /// <param name="stationId">station to check</param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public bool IsJobAvailableOnStation(StationId stationId, string jobName)
        {
            if (_stationInfo[stationId].JobList.TryGetValue(jobName, out var amount))
                return amount != 0;

            return false;
        }

        private StationId AllocateStationInfo()
        {
            return new StationId(_idCounter++);
        }
    }
}
