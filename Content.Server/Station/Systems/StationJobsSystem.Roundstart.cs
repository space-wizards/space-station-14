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

    private Dictionary<int, HashSet<string>> _jobsByWeight = default!;
    private List<int> _orderedWeights = default!;

    private void InitializeRoundStart()
    {
        _jobsByWeight = new Dictionary<int, HashSet<string>>();
        foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
        {
            if (!_jobsByWeight.ContainsKey(job.Weight))
                _jobsByWeight.Add(job.Weight, new HashSet<string>());

            _jobsByWeight[job.Weight].Add(job.ID);
        }

        _orderedWeights = _jobsByWeight.Keys.OrderByDescending(i => i).ToList();
    }

    // TODO: CLEAN THIS SHIT UP NERD
    /// <summary>
    /// Assigns jobs based on the given preferences and list of stations to assign for.
    /// This does NOT change the slots on the station, only figures out where each player should go.
    /// </summary>
    /// <param name="profiles">The profiles to use for selection.</param>
    /// <param name="stations">List of stations to assign for.</param>
    /// <returns>List of players and their assigned jobs.</returns>
    public Dictionary<NetUserId, (string, EntityUid)> AssignJobs(Dictionary<NetUserId, HumanoidCharacterProfile> profiles, IReadOnlyList<EntityUid> stations)
    {
        DebugTools.Assert(stations.Count > 0);
        DebugTools.Assert(profiles.Count > 0);
        profiles = profiles.ShallowClone();

        // Player <-> (job, station)
        var assigned = new Dictionary<NetUserId, (string, EntityUid)>(profiles.Count);

        // We reuse this collection.
        var stationSlots = new Dictionary<EntityUid, Dictionary<string, uint?>>(stations.Count);
        foreach (var station in stations)
        {
            stationSlots.Add(station, new Dictionary<string, uint?>());
        }
        // And these.
        var jobPlayerOptions = new Dictionary<string, HashSet<NetUserId>>();
        var stationTotalSlots = new Dictionary<EntityUid, int>(stations.Count);
        var stationShares = new Dictionary<EntityUid, int>(stations.Count);

        // Ok so the general algorithm:
        // We start with the highest weight jobs and work our way down.
        // Weight > Priority > Station.
        foreach (var weight in _orderedWeights)
        {
            for (var selectedPriority = JobPriority.High; selectedPriority > JobPriority.Never; selectedPriority--)
            {
                if (profiles.Count == 0)
                    goto endFunc;

                var candidates = GetPlayersJobCandidates(weight, selectedPriority, profiles);
                jobPlayerOptions.Clear(); // We reuse this collection.
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

                // We reuse this collection.
                foreach (var slots in stationSlots)
                {
                    slots.Value.Clear();
                }

                // Go through every station..
                foreach (var station in stations)
                {
                    var slots = stationSlots[station];

                    // Get all of the jobs in the selected weight category.
                    foreach (var (job, slot) in GetJobs(station))
                    {
                        if (_jobsByWeight[weight].Contains(job))
                            slots.Add(job, slot);
                    }
                }

                // Intentionally discounts the value of uncapped slots! They're only a single slot when deciding a station's share.
                // Clear for reuse.
                stationTotalSlots.Clear();
                foreach (var (station, jobs) in stationSlots)
                {
                    stationTotalSlots.Add(
                        station,
                        (int)jobs.Values.Sum(x => x ?? 1)
                        );
                }

                var totalSlots = 0;

                foreach (var (_, slot) in stationTotalSlots)
                {
                    totalSlots += slot;
                }

                if (totalSlots == 0)
                    continue; // No slots so just leave.

                // Clear for reuse.
                stationShares.Clear();
                var distributed = 0;

                foreach (var station in stations)
                {
                    // Calculates the percent share then multiplies.
                    stationShares[station] = (int)Math.Floor(((float)stationTotalSlots[station] / totalSlots) * candidates.Count);
                    distributed += stationShares[station];
                }

                // Avoids the fair share problem where if there's two stations and one player neither gets one.
                if (distributed < candidates.Count)
                {
                    var choice = _random.Pick(stations);
                    stationShares[choice] += candidates.Count - distributed;
                }

                // Actual meat, goes through each station and shakes the tree until everyone has a job.
                foreach (var station in stations)
                {
                    if (stationShares[station] == 0)
                        continue;

                    var slots = stationSlots[station];
                    var allJobs = slots.Keys.ToList();
                    _random.Shuffle(allJobs);
                    // And iterates through all it's jobs in a random order until the count settles.
                    // No, AFAIK it cannot be done any saner than this. I hate "shaking" collections as much
                    // as you do but it's what seems to be the absolute best option here.
                    // It doesn't seem to show up on the chart, perf-wise, anyway, so it's likely fine.
                    int priorCount;
                    do
                    {
                        priorCount = stationShares[station];

                        foreach (var job in allJobs)
                        {
                            if (stationShares[station] == 0)
                                break;

                            if (slots[job] != null && slots[job] == 0)
                                continue; // Can't assign this job.

                            if (!jobPlayerOptions.ContainsKey(job))
                                continue;

                            // Picking players it finds that have the job set.
                            var player = _random.Pick(jobPlayerOptions[job]);
                            AssignPlayer(player, job, station);
                            stationShares[station]--;

                            if (slots[job] != null)
                                slots[job]--;

                            if (optionsRemaining == 0)
                                goto done;
                        }
                    } while (priorCount != stationShares[station]);
                }
                done: ;
            }
        }

        endFunc:
        return assigned;
    }

    /// <summary>
    /// Attempts to assign overflow jobs to any player in allPlayersToAssign that is not in assignedJobs.
    /// </summary>
    /// <param name="assignedJobs">All assigned jobs.</param>
    /// <param name="allPlayersToAssign">All players that might need an overflow assigned.</param>
    /// <param name="profiles">Player character profiles.</param>
    public void AssignOverflowJobs(ref Dictionary<NetUserId, (string, EntityUid)> assignedJobs,
        IEnumerable<NetUserId> allPlayersToAssign, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, IReadOnlyList<EntityUid> stations)
    {
        var givenStations = stations.ToList();
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

            if (givenStations.Count == 0)
            {
                assignedJobs.Add(player, (SharedGameTicker.FallbackOverflowJob, EntityUid.Invalid));
                continue;
            }

            _random.Shuffle(givenStations);

            foreach (var station in givenStations)
            {
                // Pick a random overflow job from that station
                var overflows = GetOverflowJobs(station).ToList();
                _random.Shuffle(overflows);

                // Stations with no overflow slots should simply get skipped over.
                if (overflows.Count == 0)
                    continue;

                // If the overflow exists, put them in as it.
                assignedJobs.Add(player, (overflows[0], givenStations[0]));
                break;
            }
        }
    }

    private Dictionary<NetUserId, List<string>> GetPlayersJobCandidates(int? weight, JobPriority? selectedPriority, Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        var outputDict = new Dictionary<NetUserId, List<string>>(profiles.Count);

        foreach (var (player, profile) in profiles)
        {
            var roleBans = _roleBanManager.GetJobBans(player);

            List<string>? availableJobs = null;

            foreach (var (jobId, priority) in profile.JobPriorities)
            {
                if (!(priority == selectedPriority || selectedPriority is null))
                    continue;

                if (!_prototypeManager.TryIndex(jobId, out JobPrototype? job))
                    continue;

                if (weight is not null && job.Weight != weight.Value)
                    continue;

                if (!(roleBans == null || !roleBans.Contains(jobId)))
                    continue;

                availableJobs ??= new List<string>(profile.JobPriorities.Count);

                availableJobs.Add(jobId);
            }

            if (availableJobs is not null)
                outputDict.Add(player, availableJobs);
        }

        return outputDict;
    }
}
