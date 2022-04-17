using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Station.Systems;

// Contains code for round-start spawning.
public sealed partial class StationJobsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RoleBanManager _roleBanManager = default!;

    // TODO: make this not alloc out the ass.
    // TODO: CLEAN THIS SHIT UP NERD
    public Dictionary<NetUserId, (string, EntityUid)> AssignJobs(Dictionary<NetUserId, HumanoidCharacterProfile> profiles, IReadOnlyList<EntityUid> stations)
    {
        DebugTools.Assert(stations.Count > 0);
        DebugTools.Assert(profiles.Count > 0);
        profiles = profiles.ShallowClone();

        // Player <-> (job, station)
        var assigned = new Dictionary<NetUserId, (string, EntityUid)>();

        // Find all jobs and group them up by their weight.
        var jobsByWeight = new Dictionary<int, HashSet<string>>();
        foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
        {
            if (!jobsByWeight.ContainsKey(job.Weight))
                jobsByWeight.Add(job.Weight, new HashSet<string>());

            jobsByWeight[job.Weight].Add(job.ID);
        }

        var orderedWeights = jobsByWeight.Keys.OrderByDescending(i => i).ToList();

        // Ok so the general algorithm:
        // We start with the highest weight jobs and work our way down.
        // Weight > Priority > Station.
        foreach (var weight in orderedWeights)
        {
            for (var selectedPriority = JobPriority.High; selectedPriority > JobPriority.Never; selectedPriority--)
            {
                var candidates = GetPlayersJobCandidates(weight, selectedPriority, profiles);
                var jobPlayerOptions = new Dictionary<string, HashSet<NetUserId>>();
                var optionsRemaining = 0;

                void AssignPlayer(NetUserId player, string job, EntityUid station)
                {
                    foreach (var (_, players) in jobPlayerOptions)
                    {
                        players.Remove(player);
                    }

                    profiles.Remove(player);
                    assigned.Add(player, (job, station));

                    optionsRemaining--;
                }

                foreach (var (user, jobs) in candidates)
                {
                    foreach (var job in jobs)
                    {
                        if (!jobPlayerOptions.ContainsKey(job))
                            jobPlayerOptions.Add(job, new HashSet<NetUserId>());

                        jobPlayerOptions[job].Add(user);
                    }

                    optionsRemaining++;
                }

                var stationSlots = new Dictionary<EntityUid, Dictionary<string, uint?>>(stations.Count);
                // Go through every station..
                foreach (var station in stations)
                {
                    var slots = new Dictionary<string, uint?>();

                    // Get all of the jobs in the selected weight category.
                    foreach (var (job, slot) in GetJobs(station))
                    {
                        if (jobsByWeight[weight].Contains(job))
                            slots.Add(job, slot);
                    }

                    stationSlots.Add(station, slots);
                }

                // Intentionally discounts the value of uncapped slots! They're not considered when deciding on a station's share.
                var stationTotalSlots = new Dictionary<EntityUid, long>();
                foreach (var (station, jobs) in stationSlots)
                {
                    stationTotalSlots.Add(
                        station,
                        jobs.Values.Where(u => u is not null).Sum(x => x!.Value)
                        );
                }

                var totalSlots = stationTotalSlots.Select(x => x.Value).Sum();

                // Percent share of players each station gets.
                var stationSharesPercent = stationTotalSlots.ToDictionary(
                    x => x.Key,
                    x => (float) x.Value / totalSlots
                );

                var stationShares = new Dictionary<EntityUid, int>();
                var distributed = 0;
                foreach (var station in stations)
                {
                    stationShares[station] = (int)Math.Floor(stationSharesPercent[station] * candidates.Count);
                    distributed += stationShares[station];
                }

                // Avoids the fair share problem where if there's two stations and one player neither gets one.
                if (distributed < candidates.Count)
                {
                    var choice = _random.Pick(stations);
                    stationShares[choice] += candidates.Count - distributed;
                }

                // Actual meat, goes through each station.
                foreach (var station in stations)
                {
                    if (stationShares[station] == 0)
                        continue;

                    var allJobs = stationSlots[station].Keys.ToList();
                    _random.Shuffle(allJobs);
                    // And iterates through all it's jobs in a random order until the count settles.
                    // No, AFAIK it cannot be done any saner than this. I hate "shaking" collections as much
                    // as you do but it's what seems to be the absolute best option here.
                    var priorCount = stationShares[station];
                    do
                    {
                        foreach (var job in allJobs)
                        {
                            if (stationShares[station] == 0)
                                break;

                            if (!jobPlayerOptions.ContainsKey(job))
                                continue;

                            // Picking players it finds that have the job set.
                            var player = _random.Pick(jobPlayerOptions[job]);
                            Logger.Debug($"{player}");
                            AssignPlayer(player, job, station);
                            stationShares[station]--;

                            if (optionsRemaining == 0)
                                goto done;
                        }
                    } while (priorCount != stationShares[station]);
                }
                done: ;
            }
        }

        return assigned;
    }

    /// <summary>
    /// Attempts to assign overflow jobs to any player in allPlayersToAssign that is not in assignedJobs.
    /// </summary>
    /// <param name="assignedJobs">All assigned jobs.</param>
    /// <param name="allPlayersToAssign">All players that might need an overflow assigned.</param>
    /// <param name="profiles">Player character profiles.</param>
    public void AssignOverflowJobs(ref Dictionary<NetUserId, (string, EntityUid)> assignedJobs,
        IEnumerable<NetUserId> allPlayersToAssign, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        // For players without jobs, give them the overflow job if they have that set...
        foreach (var player in allPlayersToAssign)
        {
            if (assignedJobs.ContainsKey(player))
            {
                continue;
            }

            var profile = profiles[player];
            if (profile.PreferenceUnavailable != PreferenceUnavailableMode.SpawnAsOverflow)
                continue;

            // Pick a random station
            var stations = _stationSystem.Stations.ToList();

            if (stations.Count == 0)
            {
                assignedJobs.Add(player, (SharedGameTicker.FallbackOverflowJob, EntityUid.Invalid));
                continue;
            }

            _random.Shuffle(stations);

            foreach (var station in stations)
            {
                // Pick a random overflow job from that station
                var overflows = GetOverflowJobs(station).ToList();
                _random.Shuffle(overflows);

                // Stations with no overflow slots should simply get skipped over.
                if (overflows.Count == 0)
                    continue;

                // If the overflow exists, put them in as it.
                assignedJobs.Add(player, (overflows[0], stations[0]));
                break;
            }
        }
    }

    private IReadOnlyDictionary<NetUserId, IReadOnlyList<string>> GetPlayersJobCandidates(int? weight, JobPriority? selectedPriority, Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        return profiles.Keys.Select(player =>
            {
                var profile = profiles[player];

                var roleBans = _roleBanManager.GetJobBans(player);
                var availableJobs = profile.JobPriorities
                    .Where(j =>
                    {
                        var (jobId, priority) = j;
                        if (!_prototypeManager.TryIndex(jobId, out JobPrototype? job))
                        {
                            // Job doesn't exist, probably old data?
                            return false;
                        }

                        if (job.Weight != weight && weight is not null)
                        {
                            return false;
                        }

                        return priority == selectedPriority || selectedPriority is null;
                    })
                    .Where(p => roleBans != null && !roleBans.Contains(p.Key))
                    .Select(j => j.Key)
                    .ToList();

                return (player, availableJobs: (IReadOnlyList<string>)availableJobs);
            })
            .Where(p => p.availableJobs.Count != 0)
            .ToDictionary(x => x.player, x => x.availableJobs);
    }
}
