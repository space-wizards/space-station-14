using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.Station.Systems;

/// <summary>
/// Manages job slots for stations.
/// </summary>
[PublicAPI]
public sealed class StationJobsSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
        SubscribeLocalEvent<StationJobsComponent, StationRenamedEvent>(OnStationRenamed);
        SubscribeLocalEvent<StationJobsComponent, ComponentShutdown>(OnStationDeletion);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        _configurationManager.OnValueChanged(CCVars.GameDisallowLateJoins, _ => UpdateJobsAvailable(), true);
    }

    private void OnStationDeletion(EntityUid uid, StationJobsComponent component, ComponentShutdown args)
    {
        UpdateJobsAvailable(); // we no longer exist so the jobs list is changed.
    }

    private void OnStationInitialized(StationInitializedEvent msg)
    {
        var stationJobs = AddComp<StationJobsComponent>(msg.Station);
        var stationData = Comp<StationDataComponent>(msg.Station);

        if (stationData.MapPrototype == null)
            return;

        var mapJobList = stationData.MapPrototype.AvailableJobs;

        stationJobs.RoundStartTotalJobs = mapJobList.Values.Select(x => x[0]).Where(x => x > 0).Sum();
        stationJobs.MidRoundTotalJobs = mapJobList.Values.Select(x => x[1]).Where(x => x > 0).Sum();
        stationJobs.TotalJobs = stationJobs.MidRoundTotalJobs;
        stationJobs.JobList = mapJobList.ToDictionary(x => x.Key, x => x.Value[1] < 0 ? null : (uint?)x.Value[1]);
        stationJobs.OverflowJobs = stationData.MapPrototype.OverflowJobs.ToHashSet();
        UpdateJobsAvailable();
    }

    #region Public API

    /// <inheritdoc cref="TryAssignJob(Robust.Shared.GameObjects.EntityUid,Content.Shared.Roles.JobPrototype,Content.Server.Station.Components.StationJobsComponent?)"/>
    /// <param name="station">Station to assign a job on.</param>
    /// <param name="job">Job to assign.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    public bool TryAssignJob(EntityUid station, JobPrototype job, StationJobsComponent? stationJobs = null)
    {
        return TryAssignJob(station, job.ID, stationJobs);
    }

    /// <summary>
    /// Attempts to assign the given job once. (essentially, it decrements the slot if possible).
    /// </summary>
    /// <param name="station">Station to assign a job on.</param>
    /// <param name="jobPrototypeId">Job prototype ID to assign.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <returns>Whether or not assignment was a success.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public bool TryAssignJob(EntityUid station, string jobPrototypeId, StationJobsComponent? stationJobs = null)
    {
        return AdjustJobSlots(station, jobPrototypeId, -1, false, stationJobs);
    }

    /// <inheritdoc cref="AdjustJobSlots(Robust.Shared.GameObjects.EntityUid,Content.Shared.Roles.JobPrototype,int,bool,Content.Server.Station.Components.StationJobsComponent?)"/>
    /// <param name="station">Station to adjust the job slot on.</param>
    /// <param name="job">Job to adjust.</param>
    /// <param name="amount">Amount to adjust by.</param>
    /// <param name="createSlot">Whether or not it should create the slot if it doesn't exist.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    public bool AdjustJobSlots(EntityUid station, JobPrototype job, int amount, bool createSlot = false, StationJobsComponent? stationJobs = null)
    {
        return AdjustJobSlots(station, job.ID, amount, createSlot, stationJobs);
    }

    /// <summary>
    /// Attempts to adjust the given job slot by the amount provided.
    /// </summary>
    /// <param name="station">Station to adjust the job slot on.</param>
    /// <param name="jobPrototypeId">Job prototype ID to adjust.</param>
    /// <param name="amount">Amount to adjust by.</param>
    /// <param name="createSlot">Whether or not it should create the slot if it doesn't exist.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <returns>Whether or not slot adjustment was a success.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public bool AdjustJobSlots(EntityUid station, string jobPrototypeId, int amount, bool createSlot = false,
        StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var jobList = stationJobs.JobList;

        // This should:
        // - Return true when zero slots are added/removed.
        // - Return true when you add.
        // - Return true when you remove and do not exceed the number of slot available.
        // - Return false when you remove from a job that doesn't exist.
        // - Return false when you remove and exceed the number of slots available.
        // And additionally, if adding would add a job not previously on the manifest when createSlot is false, return false and do nothing.
        switch (jobList.ContainsKey(jobPrototypeId))
        {
            case false when amount < 0:
                return false;
            case false:
                if (!createSlot)
                    return false;
                stationJobs.TotalJobs += amount;
                jobList[jobPrototypeId] = (uint?)amount;
                UpdateJobsAvailable();
                return true;
            case true:
                // Job is unlimited so just say we adjusted it and do nothing.
                if (jobList[jobPrototypeId] == null)
                    return true;

                // Would remove more jobs than we have available.
                if (amount < 0 && jobList[jobPrototypeId] - amount < 0)
                    return false;

                stationJobs.TotalJobs += amount;

                //C# type handling moment
                if (amount > 0)
                    jobList[jobPrototypeId] += (uint)amount;
                else
                    jobList[jobPrototypeId] -= (uint)Math.Abs(amount);
                UpdateJobsAvailable();
                return true;
        }
    }

    /// <inheritdoc cref="MakeJobUnlimited(Robust.Shared.GameObjects.EntityUid,Content.Shared.Roles.JobPrototype,Content.Server.Station.Components.StationJobsComponent?)"/>
    /// <param name="station">Station to make a job unlimited on.</param>
    /// <param name="job">Job to make unlimited.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    public void MakeJobUnlimited(EntityUid station, JobPrototype job, StationJobsComponent? stationJobs = null)
    {
        MakeJobUnlimited(station, job.ID, stationJobs);
    }

    /// <summary>
    /// Makes the given job have unlimited slots.
    /// </summary>
    /// <param name="station">Station to make a job unlimited on.</param>
    /// <param name="jobPrototypeId">Job prototype ID to make unlimited.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public void MakeJobUnlimited(EntityUid station, string jobPrototypeId, StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        // Subtract out the job we're fixing to make have unlimited slots.
        if (stationJobs.JobList.ContainsKey(jobPrototypeId) && stationJobs.JobList[jobPrototypeId] != null)
            stationJobs.TotalJobs -= (int)stationJobs.JobList[jobPrototypeId]!.Value;

        stationJobs.JobList[jobPrototypeId] = null;

        UpdateJobsAvailable();
    }

    /// <inheritdoc cref="IsJobUnlimited(Robust.Shared.GameObjects.EntityUid,Content.Shared.Roles.JobPrototype,Content.Server.Station.Components.StationJobsComponent?)"/>
    /// <param name="station">Station to check.</param>
    /// <param name="job">Job to check.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    public bool IsJobUnlimited(EntityUid station, JobPrototype job, StationJobsComponent? stationJobs = null)
    {
        return IsJobUnlimited(station, job.ID, stationJobs);
    }

    /// <summary>
    /// Checks if the given job is unlimited.
    /// </summary>
    /// <param name="station">Station to check.</param>
    /// <param name="jobPrototypeId">Job prototype ID to check.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <returns>Returns if the given slot is unlimited.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public bool IsJobUnlimited(EntityUid station, string jobPrototypeId, StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        return stationJobs.JobList.TryGetValue(jobPrototypeId, out var job) && job == null;
    }

    /// <inheritdoc cref="TryGetJobSlot(Robust.Shared.GameObjects.EntityUid,Content.Shared.Roles.JobPrototype,out System.Nullable{uint},Content.Server.Station.Components.StationJobsComponent?)"/>
    /// <param name="station">Station to get slot info from.</param>
    /// <param name="job">Job to get slot info for.</param>
    /// <param name="slots">The number of slots remaining. Null if infinite.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    public bool TryGetJobSlot(EntityUid station, JobPrototype job, out uint? slots, StationJobsComponent? stationJobs = null)
    {
        return TryGetJobSlot(station, job.ID, out slots, stationJobs);
    }

    /// <summary>
    /// Returns information about the given job slot.
    /// </summary>
    /// <param name="station">Station to get slot info from.</param>
    /// <param name="jobPrototypeId">Job prototype ID to get slot info for.</param>
    /// <param name="slots">The number of slots remaining. Null if infinite.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <returns>Whether or not the slot exists.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    /// <remarks>slots will be null if the slot doesn't exist, as well, so make sure to check the return value.</remarks>
    public bool TryGetJobSlot(EntityUid station, string jobPrototypeId, out uint? slots, StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        if (stationJobs.JobList.TryGetValue(jobPrototypeId, out var job))
        {
            slots = job;
            return true;
        }
        else // Else if slot isn't present return null.
        {
            slots = null;
            return false;
        }
    }

    /// <summary>
    /// Returns all jobs available on the station.
    /// </summary>
    /// <param name="station">Station to get jobs for</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <returns>Set containing all jobs available.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public IReadOnlySet<string> GetAvailableJobs(EntityUid station, StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        return stationJobs.JobList.Where(x => x.Value != 0).Select(x => x.Key).ToHashSet();
    }

    /// <summary>
    /// Returns all overflow jobs available on the station.
    /// </summary>
    /// <param name="station">Station to get jobs for</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <returns>Set containing all overflow jobs available.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public IReadOnlySet<string> GetOverflowJobs(EntityUid station, StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        return stationJobs.OverflowJobs;
    }
    #endregion Public API

    #region Latejoin job management

    private TickerJobsAvailableEvent _cachedAvailableJobs = new TickerJobsAvailableEvent(new Dictionary<EntityUid, string>(), new Dictionary<EntityUid, Dictionary<string, uint?>>());

    private TickerJobsAvailableEvent GenerateJobsAvailableEvent()
    {
        // If late join is disallowed, return no available jobs.
        if (_gameTicker.DisallowLateJoin)
            return new TickerJobsAvailableEvent(new Dictionary<EntityUid, string>(), new Dictionary<EntityUid, Dictionary<string, uint?>>());

        var jobs = new Dictionary<EntityUid, Dictionary<string, uint?>>();
        var stationNames = new Dictionary<EntityUid, string>();

        foreach (var station in _stationSystem.Stations)
        {
            var list = Comp<StationJobsComponent>(station).JobList.ToDictionary(x => x.Key, x => x.Value);
            jobs.Add(station, list);
            stationNames.Add(station, Name(station));
        }
        return new TickerJobsAvailableEvent(stationNames, jobs);
    }

    /// <summary>
    /// Updates the cached available jobs. Moderately expensive.
    /// </summary>
    private void UpdateJobsAvailable()
    {
        _cachedAvailableJobs = GenerateJobsAvailableEvent();
        RaiseNetworkEvent(_cachedAvailableJobs, Filter.Empty().AddPlayers(_gameTicker.PlayersInLobby.Keys));
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        RaiseNetworkEvent(_cachedAvailableJobs, ev.PlayerSession.ConnectedClient);
    }

    private void OnStationRenamed(EntityUid uid, StationJobsComponent component, StationRenamedEvent args)
    {
        UpdateJobsAvailable();
    }

    #endregion
}
