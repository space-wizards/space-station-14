using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Station.Systems;

// Contains code for round-start spawning.
public sealed partial class StationJobsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    private Dictionary<int, HashSet<string>> _jobsByWeight = default!;
    private List<int> _orderedWeights = default!;

    /// <summary>
    /// Sets up some tables used by AssignJobs, including jobs sorted by their weights, and a list of weights in order from highest to lowest.
    /// </summary>
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

    /// <summary>
    /// Assigns jobs based on the given preferences and list of stations to assign for.
    /// This does NOT change the slots on the station, only figures out where each player should go.
    /// </summary>
    /// <param name="profiles">The profiles to use for selection.</param>
    /// <param name="stations">List of stations to assign for.</param>
    /// <param name="useRoundStartJobs">Whether or not to use the round-start jobs for the stations instead of their
    /// current jobs. Set to false if using this mid-round.</param>
    /// <returns>List of players and their assigned jobs.</returns>
    /// <remarks>
    /// </remarks>
    public Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)> AssignJobs(Dictionary<NetUserId, HumanoidCharacterProfile> profiles, IReadOnlyList<EntityUid> stations, bool useRoundStartJobs = true)
    {
        DebugTools.Assert(stations.Count > 0);

        InitializeRoundStart();

        if (profiles.Count == 0)
            return new();

        var unassignedProfiles = profiles.ShallowClone();

        // Player <-> (job, station)
        var assigned = new Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)>(unassignedProfiles.Count);

        // The jobs on the stations.
        var allStationsJobs = new Dictionary<EntityUid, IReadOnlyDictionary<ProtoId<JobPrototype>, int?>>();
        foreach (var station in stations)
        {
            allStationsJobs.Add(station, useRoundStartJobs ? GetRoundStartJobs(station) : GetJobs(station));
        }

        // Assign all jobs of the same weight in one batch. Start with the highest weight and work down to the lowest.
        foreach (var weight in _orderedWeights)
        {
            if (unassignedProfiles.Count == 0)
                break;

            // The jobs we're currently trying to select players for. Open slot counts will be updated
            // here as jobs are assigned.
            var currentWeightJobSlots =
                new Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>>(stations.Count);

            foreach (var station in stations)
            {
                var jobs = new Dictionary<ProtoId<JobPrototype>, int?>();

                foreach (var (job, slot) in allStationsJobs[station])
                {
                    if (_jobsByWeight[weight].Contains(job))
                        jobs.Add(job, slot);
                }

                currentWeightJobSlots.Add(station, jobs);
            }

            // Try to assign the currentWeightJobSlots to players who have them at high priority, then medium on the
            // next iteration, then low on the third & final iteration.
            for (var currentPriority = JobPriority.High; currentPriority > JobPriority.Never; currentPriority--)
            {
                if (unassignedProfiles.Count == 0)
                    break;

                // Find the unassigned players who have one or more of the current weight jobs at currentPriority
                var candidates = GetPlayersJobCandidates(weight, currentPriority, unassignedProfiles);

                // Tracks the players by job so it's easy to pick a random player for a specific job later
                var jobCandidates = new Dictionary<ProtoId<JobPrototype>, HashSet<NetUserId>>();

                foreach (var (user, jobs) in candidates)
                {
                    foreach (var job in jobs)
                    {
                        if (!jobCandidates.ContainsKey(job))
                            jobCandidates.Add(job, new HashSet<NetUserId>());

                        jobCandidates[job].Add(user);
                    }
                }

                // The share of the players each station can have for this iteration
                var stationShares = CalculateStationShares(currentWeightJobSlots, candidates.Count);

                // Actually assign jobs for one station at a time
                foreach (var station in stations)
                {
                    if (stationShares[station] == 0)
                        continue;

                    // The jobs we're selecting from for the current station.
                    var currentJobs = currentWeightJobSlots[station].Keys.ToList();

                    // We want to go through them in random order.
                    _random.Shuffle(currentJobs);

                    // Loop through the jobs repeatedly until one of the following happens:
                    // * The station has its share of players for the current weight & priority
                    // * All the players in jobCandidates have been assigned jobs
                    // * None of the remaining jobCandidates can be assigned to any of the jobs in currentJobs, due
                    //   to the jobs being full and/or the players not matching the remaining jobs
                    var stillAssigningJobs = true;
                    while (stillAssigningJobs)
                    {
                        // This will get set back to true in the inner loop when a player is assigned to a job.
                        // If no players get assigned to jobs in the inner loop, then the jobs are full and/or
                        // the players don't match the remaining jobs.
                        stillAssigningJobs = false;

                        foreach (var job in currentJobs)
                        {
                            if (stationShares[station] == 0 // The station has its share of players for the current weight & priority
                                || jobCandidates.Count == 0) // All the players in jobCandidates have been assigned jobs
                            {
                                stillAssigningJobs = false;
                                break;
                            }

                            // null indicates an uncapped job here
                            if (currentWeightJobSlots[station][job] != null && currentWeightJobSlots[station][job] == 0)
                                continue; // Can't assign this job.

                            if (!jobCandidates.ContainsKey(job))
                                continue;

                            // Pick one of the job's candidates at random
                            var player = _random.Pick(jobCandidates[job]);
                            assigned.Add(player, (job, station));

                            // Update various bookkeeping data
                            unassignedProfiles.Remove(player);
                            currentWeightJobSlots[station][job]--;
                            stationShares[station]--;
                            RemoveJobCandidate(jobCandidates, player);

                            // There was now at least one job assigned on this loop over the jobs
                            stillAssigningJobs = true;
                        }
                    }
                }
            }
        }

        return assigned;
    }

    private static void RemoveJobCandidate(Dictionary<ProtoId<JobPrototype>, HashSet<NetUserId>> jobCandidates,
        NetUserId candidateToRemove)
    {
        // Remove the player from all possible jobs as that's faster than actually checking what they have selected.
        foreach (var (k, players) in jobCandidates)
        {
            players.Remove(candidateToRemove);
            if (players.Count == 0)
                jobCandidates.Remove(k);
        }
    }

    /// <summary>
    /// Assign each station a maximum share of the candidates for the current iteration
    /// of job assigning, proportional to the number of job slots it has for said iteration.
    /// </summary>
    /// <param name="currentWeightJobs">Job slots for this iteration</param>
    /// <param name="candidateCount">How many job candidates there are for this iteration</param>
    /// <returns></returns>
    private Dictionary<EntityUid, int> CalculateStationShares(
        Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>> currentWeightJobs,
        int candidateCount
        )
    {
        // The share of the candidates each station gets.
        var stationShares = new Dictionary<EntityUid, int>(currentWeightJobs.Count);

        var stationSlots = currentWeightJobs.ToDictionary(x => x.Key, x => 0);
        foreach (var (station, jobs) in currentWeightJobs)
        {
            // Intentionally discounts the value of uncapped slots! They're only a single slot when
            // deciding a station's share.
            stationSlots[station] = jobs.Values.Sum(x => x ?? 1);
        }

        var totalSlots = stationSlots.Values.Sum();

        if (totalSlots == 0)
            return currentWeightJobs.ToDictionary(x => x.Key, x => 0); // No station wants any of the candidates

        // How many players we've distributed so far. Used to grant any remaining slots if we have leftovers.
        var distributed = 0;

        // Goes through each station and figures out how many players we should give it.
        foreach (var (station, slots) in stationSlots)
        {
            // Calculates the percent share then multiplies.
            stationShares[station] = (int)Math.Floor(((float)slots / totalSlots) * candidateCount);
            distributed += stationShares[station];
        }

        // Avoids the fair share problem where if there's two stations and one player neither gets one.
        // We do this by simply selecting a station randomly and giving it the remaining share(s).
        if (distributed < candidateCount)
        {
            var choice = _random.Pick(stationShares.Keys);
            stationShares[choice] += candidateCount - distributed;
        }

        return stationShares;
    }

    /// <summary>
    /// Attempts to assign an overflow job (eg Passenger) to any player in allPlayersToAssign that
    /// is not in assignedJobs.
    /// </summary>
    /// <param name="assignedJobs">All assigned jobs.</param>
    /// <param name="allPlayersToAssign">All players that might need an overflow assigned.</param>
    /// <param name="profiles">Player character profiles.</param>
    /// <param name="stations">The stations to consider for spawn location.</param>
    public void AssignOverflowJobs(
        ref Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)> assignedJobs,
        IEnumerable<NetUserId> allPlayersToAssign,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles,
        IReadOnlyList<EntityUid> stations)
    {
        var givenStations = stations.ToList();
        if (givenStations.Count == 0)
            return; // Don't attempt to assign them if there are no stations.
        // For players without jobs, give them the overflow job if they have that set...
        foreach (var player in allPlayersToAssign)
        {
            if (assignedJobs.ContainsKey(player))
            {
                continue;
            }

            var profile = profiles[player];
            if (profile.PreferenceUnavailable != PreferenceUnavailableMode.SpawnAsOverflow)
            {
                assignedJobs.Add(player, (null, EntityUid.Invalid));
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

    public void CalcExtendedAccess(Dictionary<EntityUid, int> jobsCount)
    {
        // Calculate whether stations need to be on extended access or not.
        foreach (var (station, count) in jobsCount)
        {
            var jobs = Comp<StationJobsComponent>(station);

            var thresh = jobs.ExtendedAccessThreshold;

            jobs.ExtendedAccess = count <= thresh;

            Log.Debug("Station {Station} on extended access: {ExtendedAccess}",
                Name(station), jobs.ExtendedAccess);
        }
    }

    /// <summary>
    /// Gets all jobs that the input players have that match the given weight and priority.
    /// </summary>
    /// <param name="weight">Weight to find, if any.</param>
    /// <param name="selectedPriority">Priority to find, if any.</param>
    /// <param name="profiles">Profiles to look in.</param>
    /// <returns>Players and a list of their matching jobs.</returns>
    private Dictionary<NetUserId, List<string>> GetPlayersJobCandidates(int? weight, JobPriority? selectedPriority, Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        var outputDict = new Dictionary<NetUserId, List<string>>(profiles.Count);

        foreach (var (player, profile) in profiles)
        {
            var roleBans = _banManager.GetJobBans(player);
            var antagBlocked = _antag.GetPreSelectedAntagSessions();
            var profileJobs = profile.JobPriorities.Keys.Select(k => new ProtoId<JobPrototype>(k)).ToList();
            var ev = new StationJobsGetCandidatesEvent(player, profileJobs);
            RaiseLocalEvent(ref ev);

            List<string>? availableJobs = null;

            foreach (var jobId in profileJobs)
            {
                var priority = profile.JobPriorities[jobId];

                if (!(priority == selectedPriority || selectedPriority is null))
                    continue;

                if (!_prototypeManager.Resolve(jobId, out var job))
                    continue;

                if (!job.CanBeAntag && (!_player.TryGetSessionById(player, out var session) || antagBlocked.Contains(session)))
                    continue;

                if (weight is not null && job.Weight != weight.Value)
                    continue;

                if (!(roleBans == null || !roleBans.Contains(jobId))) //TODO: Replace with IsRoleBanned
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
