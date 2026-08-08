using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Station.Systems;

// Contains code for round-start spawning.
public sealed partial class StationJobsSystem
{
    [Dependency] private IBanManager _banManager = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;

    private int GetJobWeight(EntityUid station, JobPrototype job)
    {
        var jobWeights = TryComp<StationDataComponent>(station, out var stationData)
            ? stationData.JobWeights
            : null;

        return TryGetJobWeight(job, jobWeights, out var weight) ? weight : 0;
    }

    /// <summary>
    /// Resolves a job's map-specific weight, falling back to the global default profile.
    /// </summary>
    /// <returns>False when neither the map profile nor the global profile defines a weight for this job.</returns>
    public bool TryGetJobWeight(
        JobPrototype job,
        ProtoId<JobWeightPrototype>? mapWeights,
        out int weight)
    {
        if (mapWeights != null
            && ProtoMan.TryIndex(mapWeights.Value, out var mapProfile)
            && mapProfile.Weights.TryGetValue(job.ID, out weight))
        {
            return true;
        }

        if (ProtoMan.TryIndex(JobWeightPrototype.Default, out var defaultProfile)
            && defaultProfile.Weights.TryGetValue(job.ID, out weight))
        {
            return true;
        }

        weight = default;
        return false;
    }

    /// <summary>
    /// Returns whether the global fallback job-weight profile is available.
    /// </summary>
    public bool HasDefaultJobWeights()
    {
        return ProtoMan.HasIndex<JobWeightPrototype>(JobWeightPrototype.Default);
    }

    /// <summary>
    /// Assigns jobs based on the given preferences and list of stations to assign for.
    /// This does NOT change the slots on the station, only figures out where each player should go.
    /// </summary>
    /// <param name="profiles">The profiles to use for selection.</param>
    /// <param name="stations">List of stations to assign for.</param>
    /// <param name="useRoundStartJobs">Whether or not to use the round-start minimum jobs for the stations.</param>
    /// <returns>List of players and their assigned jobs.</returns>
    /// <remarks>
    /// You probably shouldn't use useRoundStartJobs mid-round if the station has been available to join,
    /// as there may end up being more round-start slots than available slots, which can cause weird behavior.
    /// Round-start allocation attempts each station's minimum roles first, ordered by the station's job weights.
    /// Unpreferred minimum roles can use an eligible random player when configured to do so.
    /// It then considers remaining players in random order and gives each their highest available preference.
    /// </remarks>
    public Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)> AssignJobs(
        Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
        IReadOnlyList<EntityUid> stations,
        bool useRoundStartJobs = true)
    {
        DebugTools.Assert(stations.Count > 0);

        if (profiles.Count == 0)
            return new();

        // We need to modify this collection later, so make a copy of it.
        profiles = profiles.ShallowClone();

        // Player <-> (job, station)
        var assigned = new Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)>(profiles.Count);

        // The maximum jobs left on each station. This is modified as players are assigned.
        var stationJobs = new Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>>();
        var stationMinimumJobs = new Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>>();
        foreach (var station in stations)
        {
            stationJobs.Add(station, GetJobs(station).ToDictionary(x => x.Key, x => x.Value));
            stationMinimumJobs.Add(
                station,
                useRoundStartJobs
                    ? GetRoundStartJobs(station)
                    : new Dictionary<ProtoId<JobPrototype>, int?>());
        }

        // Jobs assigned after this point must satisfy bans, antag restrictions, and any other candidate filter.
        // The minimum phase selects players for a job*, and the maximum phase selects jobs for a player.
        var jobCandidates = GetJobCandidates(profiles);
        var playerCandidates = GetPlayerCandidates(jobCandidates);

        // Phase one: complete every required role on a station before considering the next station.
        // Within a station, job priority win over player preference; player preference breaks ties between candidates.
        var jobFallback = _configurationManager.GetCVar(CCVars.GameMinimumJobFallback);

        foreach (var station in stations)
        {
            var requiredJobs = stationMinimumJobs[station]
                .Where(x => x.Value is > 0)
                .OrderByDescending(x => GetJobWeight(station, ProtoMan.Index(x.Key)))
                .ThenBy(x => x.Key.Id)
                .ToList();

            foreach (var (job, minimum) in requiredJobs)
            {
                for (var assignedToJob = 0; assignedToJob < minimum!.Value && profiles.Count > 0; assignedToJob++)
                {
                    if (stationJobs[station][job] is <= 0)
                        break;

                    if (!TryPickCandidate(job, jobCandidates, out var player) &&
                        !TryPickMinimumJobFallbackCandidate(
                            job, profiles, jobFallback, out player))
                    {
                        break;
                    }

                    AssignPlayer(player, job, station, stationJobs, jobCandidates, playerCandidates, profiles, assigned);
                }
            }
        }

        // Phase two: each remaining player gets their highest available preference. Shuffle the player order and
        // equal-priority jobs so contention is still fair, while preserving station-by-station allocation.
        foreach (var station in stations)
        {
            var players = profiles.Keys.ToList();
            _random.Shuffle(players);

            foreach (var player in players)
            {
                if (TryPickJob(player, station, stationJobs, playerCandidates, out var job))
                    AssignPlayer(player, job, station, stationJobs, jobCandidates, playerCandidates, profiles, assigned);
            }
        }

        return assigned;
    }

    private void RemovePlayerFromCandidates(
        NetUserId player,
        Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>> jobCandidates,
        Dictionary<NetUserId, Dictionary<JobPriority, List<ProtoId<JobPrototype>>>> playerCandidates)
    {
        foreach (var priorities in jobCandidates.Values)
        {
            foreach (var players in priorities.Values)
            {
                players.Remove(player);
            }
        }

        playerCandidates.Remove(player);
    }

    private bool TryPickCandidate(
        ProtoId<JobPrototype> job,
        Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>> jobCandidates,
        out NetUserId player)
    {
        if (!jobCandidates.TryGetValue(job, out var candidates))
        {
            player = default;
            return false;
        }

        for (var priority = JobPriority.High; priority > JobPriority.Never; priority--)
        {
            if (!candidates.TryGetValue(priority, out var players) || players.Count == 0)
                continue;

            player = _random.Pick(players);
            return true;
        }

        player = default;
        return false;
    }

    private bool TryPickJob(
        NetUserId player,
        EntityUid station,
        Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>> stationJobs,
        Dictionary<NetUserId, Dictionary<JobPriority, List<ProtoId<JobPrototype>>>> playerCandidates,
        out ProtoId<JobPrototype> job)
    {
        if (!playerCandidates.TryGetValue(player, out var candidates))
        {
            job = default;
            return false;
        }

        for (var priority = JobPriority.High; priority > JobPriority.Never; priority--)
        {
            if (!candidates.TryGetValue(priority, out var jobs))
                continue;

            var availableJobs = jobs
                .Where(jobId => stationJobs[station].TryGetValue(jobId, out var slots) && slots is null or > 0)
                .ToList();
            if (availableJobs.Count == 0)
                continue;

            job = _random.Pick(availableJobs);
            return true;
        }

        job = default;
        return false;
    }

    private void AssignPlayer(
        NetUserId player,
        ProtoId<JobPrototype> job,
        EntityUid station,
        Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>> stationJobs,
        Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>> jobCandidates,
        Dictionary<NetUserId, Dictionary<JobPriority, List<ProtoId<JobPrototype>>>> playerCandidates,
        Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
        Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)> assigned)
    {
        if (stationJobs[station][job] is { } slots)
            stationJobs[station][job] = slots - 1;

        RemovePlayerFromCandidates(player, jobCandidates, playerCandidates);
        profiles.Remove(player);
        assigned.Add(player, (job, station));
    }

    /// <summary>
    /// Attempts to assign overflow jobs to any player in allPlayersToAssign that is not in assignedJobs.
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
                continue;

            var profile = profiles[player];
            if (profile.PreferenceUnavailable != PreferenceUnavailableMode.SpawnAsOverflow)
            {
                assignedJobs.Add(player, (null, EntityUid.Invalid));
                continue;
            }

            _random.Shuffle(givenStations);

            foreach (var station in givenStations)
            {
                // Pick a random overflow job from that station.
                var overflows = GetOverflowJobs(station).ToList();
                _random.Shuffle(overflows);

                // Stations with no overflow slots should simply get skipped over.
                if (overflows.Count == 0)
                    continue;

                assignedJobs.Add(player, (overflows[0], station));
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
    /// Gets all jobs that the input players can receive, grouped by their selected preference priority.
    /// </summary>
    /// <param name="profiles">Profiles to look in.</param>
    /// <returns>Jobs and their eligible players, grouped by player preference.</returns>
    private Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>> GetJobCandidates(
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        var outputDict = new Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>>();

        var antags = _antag.GetAntagJobs();

        foreach (var (player, profile) in profiles)
        {
            var roleBans = _banManager.GetJobBans(player);
            var profileJobs = profile.JobPriorities.Keys.Select(k => new ProtoId<JobPrototype>(k)).ToList();
            var ev = new StationJobsGetCandidatesEvent(player, profileJobs);
            RaiseLocalEvent(ref ev);

            // Shouldn't happen but you know :P
            if (!_player.TryGetSessionById(player, out var session))
                continue;

            var (whitelist, blacklist) = antags.GetValueOrDefault(session);

            foreach (var jobId in profileJobs)
            {
                if (!profile.JobPriorities.TryGetValue(jobId, out var priority) || priority == JobPriority.Never)
                    continue;

                if (!ProtoMan.Resolve(jobId, out _))
                    continue;

                if (whitelist != null && !whitelist.Contains(jobId))
                    continue;

                if (blacklist != null && blacklist.Contains(jobId))
                    continue;

                if (!(roleBans == null || !roleBans.Contains(jobId))) //TODO: Replace with IsRoleBanned
                    continue;

                if (!outputDict.TryGetValue(jobId, out var priorities))
                {
                    priorities = new Dictionary<JobPriority, HashSet<NetUserId>>();
                    outputDict.Add(jobId, priorities);
                }

                if (!priorities.TryGetValue(priority, out var players))
                {
                    players = new HashSet<NetUserId>();
                    priorities.Add(priority, players);
                }

                players.Add(player);
            }
        }

        return outputDict;
    }

    /// <summary>
    /// Tries the configured fallback for a required role that has no direct preference candidates.
    /// </summary>
    private bool TryPickMinimumJobFallbackCandidate(
        ProtoId<JobPrototype> job,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles,
        MinimumJobFallback fallback,
        out NetUserId player)
    {
        switch (fallback)
        {
            case MinimumJobFallback.SameDepartment:
                return TryPickSameDepartmentCandidate(job, profiles, out player);

            case MinimumJobFallback.AnyEligiblePlayer:
                if (TryPickSameDepartmentCandidate(job, profiles, out player))
                    return true;

                return TryPickCandidateIgnoringPreferences(job, profiles, out player);

            default:
                player = default;
                return false;
        }
    }

    /// <summary>
    /// Gets a random eligible player who prefers a role in the target job's primary department.
    /// </summary>
    private bool TryPickSameDepartmentCandidate(
        ProtoId<JobPrototype> job,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles,
        out NetUserId player)
    {
        if (!_jobs.TryGetPrimaryDepartment(job.Id, out var department))
        {
            player = default;
            return false;
        }

        var matchingProfiles = profiles
            .Where(pair => pair.Value.JobPriorities.Any(preference =>
                preference.Value != JobPriority.Never && department.Roles.Contains(preference.Key)))
            .ToDictionary();
        return TryPickCandidateIgnoringPreferences(job, matchingProfiles, out player);
    }

    /// <summary>
    /// Gets a random eligible player for a required role without requiring a preference for that role.
    /// </summary>
    /// <remarks>
    /// This deliberately uses the same candidate-filter event as normal assignment so role timers and whitelists
    /// still apply. The only criterion omitted is the player's job preference.
    /// </remarks>
    private bool TryPickCandidateIgnoringPreferences(
        ProtoId<JobPrototype> job,
        IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles,
        out NetUserId player)
    {
        if (!ProtoMan.HasIndex(job))
        {
            player = default;
            return false;
        }

        var candidates = new HashSet<NetUserId>();
        var antags = _antag.GetAntagJobs();

        foreach (var (userId, _) in profiles)
        {
            if (!_player.TryGetSessionById(userId, out var session))
                continue;

            var jobs = new List<ProtoId<JobPrototype>> { job };
            var ev = new StationJobsGetCandidatesEvent(userId, jobs);
            RaiseLocalEvent(ref ev);

            if (!jobs.Contains(job))
                continue;

            var roleBans = _banManager.GetJobBans(userId);
            var (whitelist, blacklist) = antags.GetValueOrDefault(session);
            if ((whitelist != null && !whitelist.Contains(job)) ||
                (blacklist != null && blacklist.Contains(job)) ||
                (roleBans != null && roleBans.Contains(job)))
            {
                continue;
            }

            candidates.Add(userId);
        }

        if (candidates.Count > 0)
        {
            player = _random.Pick(candidates);
            return true;
        }

        player = default;
        return false;
    }

    /// <summary>
    /// Builds the inverse candidate index used by the player-first maximum-slot phase.
    /// </summary>
    private static Dictionary<NetUserId, Dictionary<JobPriority, List<ProtoId<JobPrototype>>>> GetPlayerCandidates(
        Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>> jobCandidates)
    {
        var output = new Dictionary<NetUserId, Dictionary<JobPriority, List<ProtoId<JobPrototype>>>>();
        foreach (var (job, priorities) in jobCandidates)
        {
            foreach (var (priority, players) in priorities)
            {
                foreach (var player in players)
                {
                    if (!output.TryGetValue(player, out var playerPriorities))
                    {
                        playerPriorities = new Dictionary<JobPriority, List<ProtoId<JobPrototype>>>();
                        output.Add(player, playerPriorities);
                    }

                    if (!playerPriorities.TryGetValue(priority, out var jobs))
                    {
                        jobs = new List<ProtoId<JobPrototype>>();
                        playerPriorities.Add(priority, jobs);
                    }

                    jobs.Add(job);
                }
            }
        }

        return output;
    }
}
