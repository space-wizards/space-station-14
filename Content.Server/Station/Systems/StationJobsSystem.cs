using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;

namespace Content.Server.Station.Systems;

[PublicAPI]
public sealed class StationJobsSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
    }

    private void OnStationInitialized(StationInitializedEvent msg, EntitySessionEventArgs args)
    {
        var stationJobs = AddComp<StationJobsComponent>(msg.Station);
        var stationData = Comp<StationDataComponent>(msg.Station);

        if (stationData.MapPrototype == null)
            return;

        var mapJobList = stationData.MapPrototype.AvailableJobs;

        stationJobs.RoundStartTotalJobs = mapJobList.Values.Select(x => x[0]).Where(x => x > 0).Sum();
        stationJobs.MidRoundTotalJobs = mapJobList.Values.Select(x => x[1]).Where(x => x > 0).Sum();
        stationJobs.JobList = mapJobList.ToDictionary(x => x.Key, x => x.Value[1]);
    }

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
            case false when amount == 0:
                return createSlot;
            case false:
                if (!createSlot)
                    return false;
                stationJobs.TotalJobs += amount;
                jobList[jobPrototypeId] = amount;
                return true;
            case true:
                // Job is unlimited so just say we adjusted it and do nothing.
                if (jobList[jobPrototypeId] == -1)
                    return true;

                // Would remove more jobs than we have available.
                if (amount < 0 && jobList[jobPrototypeId] - amount < 0)
                    return false;

                stationJobs.TotalJobs += amount;
                jobList[jobPrototypeId] += amount;
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
        if (stationJobs.JobList.ContainsKey(jobPrototypeId))
        {
            stationJobs.TotalJobs -= stationJobs.JobList[jobPrototypeId];
        }

        stationJobs.JobList[jobPrototypeId] = -1;
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

        return stationJobs.JobList.TryGetValue(jobPrototypeId, out var job) && job == -1;
    }

    /// <inheritdoc cref="GetJobSlot(Robust.Shared.GameObjects.EntityUid,Content.Shared.Roles.JobPrototype,Content.Server.Station.Components.StationJobsComponent?)"/>
    /// <param name="station">Station to get slot info from.</param>
    /// <param name="job">Job to get slot info for.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    public int? GetJobSlot(EntityUid station, JobPrototype job, StationJobsComponent? stationJobs = null)
    {
        return GetJobSlot(station, job.ID, stationJobs);
    }

    /// <summary>
    /// Returns information about the given job slot.
    /// </summary>
    /// <param name="station">Station to get slot info from.</param>
    /// <param name="jobPrototypeId">Job prototype ID to get slot info for.</param>
    /// <param name="stationJobs">Resolve pattern, station jobs component of the station.</param>
    /// <returns>An integer with the available slot count, if the slot exists.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public int? GetJobSlot(EntityUid station, string jobPrototypeId, StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        if (stationJobs.JobList.TryGetValue(jobPrototypeId, out var job))
        {
            return job;
        }
        else // Else if slot isn't present return null.
        {
            return null;
        }
    }
}
