using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.Antag;
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
    [Dependency] private readonly IServerPreferencesManager _serverPreferences = default!;

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
    /// <param name="userIdsIn">Set of the UserIds we will be attempting to assign.</param>
    /// <param name="stations">List of stations to assign for.</param>
    /// <param name="useRoundStartJobs">Whether or not to use the round-start jobs for the stations instead of their current jobs.</param>
    /// <returns>List of players and their assigned jobs.</returns>
    /// <remarks>
    /// You probably shouldn't use useRoundStartJobs mid-round if the station has been available to join,
    /// as there may end up being more round-start slots than available slots, which can cause weird behavior.
    /// A warning to all who enter ye cursed lands: This function is long and mildly incomprehensible. Best used without touching.
    /// </remarks>
    public Dictionary<NetUserId, (ProtoId<JobPrototype>? job, EntityUid station)> AssignJobs(IReadOnlySet<NetUserId> userIdsIn, IReadOnlyList<EntityUid> stations, bool useRoundStartJobs = true)
    {
        DebugTools.Assert(stations.Count > 0);

        InitializeRoundStart();

        if (userIdsIn.Count == 0)
            return new();

        // We need to modify this collection later, so make a copy of it.
        var userIds = userIdsIn.ToHashSet();

        // Player <-> (job, station)
        var assigned = new Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)>(userIds.Count);

        // The jobs left on the stations. This collection is modified as jobs are assigned to track what's available.
        var stationJobs = new Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>>();
        foreach (var station in stations)
        {
            if (useRoundStartJobs)
            {
                stationJobs.Add(station, GetRoundStartJobs(station).ToDictionary(x => x.Key, x => x.Value));
            }
            else
            {
                stationJobs.Add(station, GetJobs(station).ToDictionary(x => x.Key, x => x.Value));
            }
        }


        // We reuse this collection. It tracks what jobs we're currently trying to select players for.
        var currentlySelectingJobs = new Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>>(stations.Count);
        foreach (var station in stations)
        {
            currentlySelectingJobs.Add(station, new Dictionary<ProtoId<JobPrototype>, int?>());
        }

        // And these.
        // Tracks what players are available for a given job in the current iteration of selection.
        var jobPlayerOptions = new Dictionary<ProtoId<JobPrototype>, HashSet<NetUserId>>();
        // Tracks the total number of slots for the given stations in the current iteration of selection.
        var stationTotalSlots = new Dictionary<EntityUid, int>(stations.Count);
        // The share of the players each station gets in the current iteration of job selection.
        var stationShares = new Dictionary<EntityUid, int>(stations.Count);

        // Ok so the general algorithm:
        // We start with the highest weight jobs and work our way down. We filter jobs by weight when selecting as well.
        // Weight > Priority > Station.
        foreach (var weight in _orderedWeights)
        {
            for (var selectedPriority = JobPriority.High; selectedPriority > JobPriority.Never; selectedPriority--)
            {
                if (userIds.Count == 0)
                    goto endFunc;

                var candidates = GetPlayersJobCandidates(weight, selectedPriority, userIds);

                var optionsRemaining = 0;

                // Assigns a player to the given station, updating all the bookkeeping while at it.
                void AssignPlayer(NetUserId player, ProtoId<JobPrototype> job, EntityUid station)
                {
                    // Remove the player from all possible jobs as that's faster than actually checking what they have selected.
                    foreach (var (k, players) in jobPlayerOptions)
                    {
                        players.Remove(player);
                        if (players.Count == 0)
                            jobPlayerOptions.Remove(k);
                    }

                    stationJobs[station][job]--;
                    userIds.Remove(player);
                    assigned.Add(player, (job, station));

                    optionsRemaining--;
                }

                jobPlayerOptions.Clear(); // We reuse this collection.

                // Goes through every candidate, and adds them to jobPlayerOptions, so that the candidate players
                // have an index sorted by job. We use this (much) later when actually assigning people to randomly
                // pick from the list of candidates for the job.
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

                // We reuse this collection, so clear it's children.
                foreach (var slots in currentlySelectingJobs)
                {
                    slots.Value.Clear();
                }

                // Go through every station..
                foreach (var station in stations)
                {
                    var slots = currentlySelectingJobs[station];

                    // Get all of the jobs in the selected weight category.
                    foreach (var (job, slot) in stationJobs[station])
                    {
                        if (_jobsByWeight[weight].Contains(job))
                            slots.Add(job, slot);
                    }
                }


                // Clear for reuse.
                stationTotalSlots.Clear();

                // Intentionally discounts the value of uncapped slots! They're only a single slot when deciding a station's share.
                foreach (var (station, jobs) in currentlySelectingJobs)
                {
                    stationTotalSlots.Add(
                        station,
                        (int)jobs.Values.Sum(x => x ?? 1)
                        );
                }

                var totalSlots = 0;

                // LINQ moment.
                // totalSlots = stationTotalSlots.Sum(x => x.Value);
                foreach (var (_, slot) in stationTotalSlots)
                {
                    totalSlots += slot;
                }

                if (totalSlots == 0)
                    continue; // No slots so just move to the next iteration.

                // Clear for reuse.
                stationShares.Clear();

                // How many players we've distributed so far. Used to grant any remaining slots if we have leftovers.
                var distributed = 0;

                // Goes through each station and figures out how many players we should give it for the current iteration.
                foreach (var station in stations)
                {
                    // Calculates the percent share then multiplies.
                    stationShares[station] = (int)Math.Floor(((float)stationTotalSlots[station] / totalSlots) * candidates.Count);
                    distributed += stationShares[station];
                }

                // Avoids the fair share problem where if there's two stations and one player neither gets one.
                // We do this by simply selecting a station randomly and giving it the remaining share(s).
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

                    // The jobs we're selecting from for the current station.
                    var currStationSelectingJobs = currentlySelectingJobs[station];
                    // We only need this list because we need to go through this in a random order.
                    // Oh the misery, another allocation.
                    var allJobs = currStationSelectingJobs.Keys.ToList();
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

                            if (currStationSelectingJobs[job] != null && currStationSelectingJobs[job] == 0)
                                continue; // Can't assign this job.

                            if (!jobPlayerOptions.ContainsKey(job))
                                continue;

                            // Picking players it finds that have the job set.
                            var player = _random.Pick(jobPlayerOptions[job]);
                            AssignPlayer(player, job, station);
                            stationShares[station]--;

                            if (currStationSelectingJobs[job] != null)
                                currStationSelectingJobs[job]--;

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
    /// <param name="players">Players to select from</param>
    /// <returns>Players and a list of their matching jobs.</returns>
    private Dictionary<NetUserId, List<string>> GetPlayersJobCandidates(int? weight, JobPriority? selectedPriority, ICollection<NetUserId> players)
    {
        var outputDict = new Dictionary<NetUserId, List<string>>(players.Count);

        foreach (var player in players)
        {
            if (!_player.TryGetSessionById(player, out var session))
                continue;

            var roleBans = _banManager.GetJobBans(player);
            var isPreselectedAntag = _antag.GetPreSelectedAntagSessions().Contains(session);
            var preselectedAntags = _antag.GetPreSelectedAntagDefinitions(session);

            // Get all the jobs that a player has selected with a priority greater than Never and also that they
            // have an enabled character with that job preference selected
            var playerPrefs = _serverPreferences.GetPreferences(player);
            var playerJobs = playerPrefs.JobPriorities;
            var allCharacterJobs = new HashSet<ProtoId<JobPrototype>>();
            foreach (var profile in playerPrefs.Characters.Values)
            {
                if (profile is not HumanoidCharacterProfile { Enabled: true } humanoid)
                    continue;
                allCharacterJobs.UnionWith(humanoid.JobPreferences);
            }
            var filteredPlayerJobs = new HashSet<ProtoId<JobPrototype>>();
            foreach (var (job, priority) in playerJobs)
            {
                if (!(priority == selectedPriority || selectedPriority is null))
                    continue;
                if (!allCharacterJobs.Contains(job))
                    continue;
                filteredPlayerJobs.Add(job);
            }

            // Remove jobs that the player in ineligible for
            var profileJobs = filteredPlayerJobs.ToList();
            var ev = new StationJobsGetCandidatesEvent(player, profileJobs);
            RaiseLocalEvent(ref ev);

            List<string>? availableJobs = null;

            foreach (var jobId in profileJobs)
            {
                var priority = playerJobs[jobId];

                if (!(priority == selectedPriority || selectedPriority is null))
                    continue;

                if (!_prototypeManager.Resolve(jobId, out var job))
                    continue;

                // If we're an antag but the job can't be an antag, don't allow this job
                if (isPreselectedAntag && !job.CanBeAntag)
                    continue;

                // If we're an antag, make sure that we have a character that is eligible to
                // become all of our selected antags
                if (isPreselectedAntag && !preselectedAntags.All(antag =>
                        _antag.HasPrimaryAntagPreference(session, antag, AntagSelectionTime.IntraPlayerSpawn, job)))
                    continue;

                if (weight is not null && job.Weight != weight.Value)
                    continue;

                if (!(roleBans == null || !roleBans.Contains(jobId))) //TODO: Replace with IsRoleBanned
                    continue;

                availableJobs ??= new List<string>(playerJobs.Count);
                availableJobs.Add(jobId);
            }

            if (availableJobs is not null)
                outputDict.Add(player, availableJobs);
        }

        return outputDict;
    }
}
