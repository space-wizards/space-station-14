using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    // This code is responsible for the assigning & picking of jobs.
    public sealed partial class GameTicker
    {
        [ViewVariables]
        private readonly List<ManifestEntry> _manifest = new();

        [ViewVariables]
        private readonly Dictionary<string, int> _spawnedPositions = new();

        private Dictionary<IPlayerSession, (string, EntityUid)> AssignJobs(List<IPlayerSession> availablePlayers,
            Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
        {
            var assigned = new Dictionary<IPlayerSession, (string, EntityUid)>();

            List<(IPlayerSession, List<string>)> GetPlayersJobCandidates(bool heads, JobPriority i)
            {
                return availablePlayers.Select(player =>
                    {
                        var profile = profiles[player.UserId];

                        var roleBans = _roleBanManager.GetJobBans(player.UserId);
                        var availableJobs = profile.JobPriorities
                            .Where(j =>
                            {
                                var (jobId, priority) = j;
                                if (!_prototypeManager.TryIndex(jobId, out JobPrototype? job))
                                {
                                    // Job doesn't exist, probably old data?
                                    return false;
                                }

                                if (job.IsHead != heads)
                                {
                                    return false;
                                }

                                return priority == i;
                            })
                            .Where(p => roleBans != null && !roleBans.Contains(p.Key))
                            .Select(j => j.Key)
                            .ToList();

                        return (player, availableJobs);
                    })
                    .Where(p => p.availableJobs.Count != 0)
                    .ToList();
            }

            void ProcessJobs(bool heads, Dictionary<string, int> availablePositions, EntityUid id, JobPriority i)
            {
                var candidates = GetPlayersJobCandidates(heads, i);

                foreach (var (candidate, jobs) in candidates)
                {
                    while (jobs.Count != 0)
                    {
                        var picked = _robustRandom.Pick(jobs);

                        var openPositions = availablePositions.GetValueOrDefault(picked, 0);
                        if (openPositions == 0)
                        {
                            jobs.Remove(picked);
                            continue;
                        }

                        availablePositions[picked] -= 1;
                        assigned.Add(candidate, (picked, id));
                        break;
                    }
                }

                availablePlayers.RemoveAll(a => assigned.ContainsKey(a));
            }

            // Current strategy is to fill each station one by one.
            foreach (var station in _stationSystem.Stations)
            {
                // Get the ROUND-START job list.
                var availablePositions = Comp<StationDataComponent>(station).MapPrototype?.AvailableJobs.ToDictionary(x => x.Key, x => x.Value[0]);

                if (availablePositions is null)
                    continue;

                for (var i = JobPriority.High; i > JobPriority.Never; i--)
                {
                    // Process jobs possible for heads...
                    ProcessJobs(true, availablePositions, station, i);
                    // and then jobs that are not heads.
                    ProcessJobs(false, availablePositions, station, i);
                }
            }

            return assigned;
        }

        private string? PickBestAvailableJob(IPlayerSession playerSession, HumanoidCharacterProfile profile,
            EntityUid station)
        {
            if (station == EntityUid.Invalid)
                return null;

            var available = _stationJobs.GetAvailableJobs(station);

            bool TryPick(JobPriority priority, [NotNullWhen(true)] out string? jobId)
            {
                var roleBans = _roleBanManager.GetJobBans(playerSession.UserId);
                var filtered = profile.JobPriorities
                    .Where(p => p.Value == priority)
                    .Where(p => roleBans != null && !roleBans.Contains(p.Key))
                    .Select(p => p.Key)
                    .ToList();

                if (filtered.Count != 0)
                {
                    jobId = _robustRandom.Pick(filtered);
                    return true;
                }

                jobId = default;
                return false;
            }

            if (TryPick(JobPriority.High, out var picked))
            {
                return picked;
            }

            if (TryPick(JobPriority.Medium, out picked))
            {
                return picked;
            }

            if (TryPick(JobPriority.Low, out picked))
            {
                return picked;
            }

            var overflows = _stationJobs.GetOverflowJobs(station);
            return overflows.Count != 0 ? _robustRandom.Pick(overflows) : null;
        }

        [Conditional("DEBUG")]
        private void InitializeJobController()
        {
            // Verify that the overflow role exists and has the correct name.
            var role = _prototypeManager.Index<JobPrototype>(FallbackOverflowJob);
            DebugTools.Assert(role.Name == Loc.GetString(FallbackOverflowJobName),
                "Overflow role does not have the correct name!");
        }

        private void AddSpawnedPosition(string jobId)
        {
            _spawnedPositions[jobId] = _spawnedPositions.GetValueOrDefault(jobId, 0) + 1;
        }
    }
}
