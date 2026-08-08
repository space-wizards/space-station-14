// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.DeadSpace.PersonnelRecords.Components;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using SharedPersonnelRecordsConsoleComponent = Content.Shared.DeadSpace.PersonnelRecords.Components.PersonnelRecordsConsoleComponent;

namespace Content.Server.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Returns a vacated job's slot to the station's pool once a Demotion/Dismissal order executes
/// (§2.4 "Как не дать абузить слоты"). Deliberately its own system, subscribed to
/// <see cref="PersonnelOrderExecutedEvent"/> rather than living inside the completion detector -
/// same "event, not a direct call" split as the criminal-records bridge.
///
/// The naive check ("free slots ≤ round-start count") doesn't work, because the ID card console
/// can hand out a job without ever touching the slot pool (promoting an assistant to officer costs
/// nothing). The invariant actually enforced here is:
///
///     occupied(job) + free(job) ≤ round-start(job)
///
/// A slot is only ever added back if, after adding it, that inequality would still hold - i.e.
/// only if a slot was actually vacated by someone who was really occupying one. See the abuse
/// vector table in the design doc (§2.4) for why this - and not the simpler check - is required.
///
/// That invariant is only half the story though: nothing stopped the *same* freed slot from being
/// abused the other way. If the dismissed/demoted person (or anyone else) later gets moved back
/// into that job through the ID card console - which, same as above, never touches the slot pool -
/// occupied goes back up but the free slot never goes back down, so the station ends up with room
/// for a second occupant of a job that's supposed to be capped at one (<see cref="OnJobAssigned"/>).
/// <see cref="PersonnelSlotTrackingComponent"/> on the station is what makes it safe to claw that
/// slot back: it only ever reclaims slots this system itself handed out, never a station's
/// round-start allotment or a free slot that exists for an unrelated reason.
/// </summary>
public sealed class PersonnelVacancySystem : EntitySystem
{
    /// <summary>
    /// Access that always bypasses <see cref="OnJobAssignmentAttempt"/> - same precedent as the
    /// Captain/IAA/BlueShieldOfficer protected-jobs check in <c>PersonnelRecordsConsoleSystem</c>:
    /// Central Command can do what a station-bound console never should.
    /// </summary>
    private static readonly ProtoId<AccessLevelPrototype> CentComBypassAccess = "CentralCommand";

    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly PersonnelRecordsConsoleSystem _console = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PersonnelOrderExecutedEvent>(OnOrderExecuted);
        SubscribeLocalEvent<IdCardJobAssignmentAttemptEvent>(OnJobAssignmentAttempt);
        SubscribeLocalEvent<IdCardJobAssignedEvent>(OnJobAssigned);
    }

    /// <summary>
    /// Closes the gap <see cref="OnJobAssigned"/> can't: if a new hire already took the slot a
    /// Demotion/Dismissal freed (the normal, intended outcome), there's nothing left in the free
    /// pool to reclaim when the original person - or anyone else - gets reassigned back into that
    /// job through the ID card console, and the station ends up over its round-start cap for real
    /// (§2.4 abuse vector: dismiss -&gt; new hire fills the vacancy -&gt; reinstate the original).
    ///
    /// Rather than trying to undo that after the fact, this vetoes the assignment itself, but only
    /// in exactly that situation - a job this system has an outstanding "extra slot" debt for, with
    /// no free slot currently sitting there to legitimately consume. Any other job, or the same job
    /// while a freed slot is still unclaimed, goes through completely untouched: promoting an
    /// assistant to officer, or hiring extra officers with no history of a dismissal, never hits
    /// this check at all.
    /// </summary>
    private void OnJobAssignmentAttempt(IdCardJobAssignmentAttemptEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (_access.FindAccessTags(ev.Actor).Contains(CentComBypassAccess))
            return;

        if (_station.GetOwningStation(ev.TargetId) is not { } station)
            return;

        if (!TryComp<PersonnelSlotTrackingComponent>(station, out var tracking))
            return;

        if (!tracking.ExtraSlotsFreed.TryGetValue(ev.NewJob, out var extra) || extra <= 0)
            return;

        // A freed slot is still sitting there unclaimed - this assignment is exactly what it's for,
        // let it through (OnJobAssigned reclaims it once the write succeeds).
        if (_stationJobs.TryGetJobSlot(station, ev.NewJob.Id, out var freeSlots) && freeSlots is { } free && free > 0)
            return;

        ev.Cancel();
        _popup.PopupEntity(Loc.GetString("personnel-records-console-job-assignment-blocked"), ev.TargetId, ev.Actor);
    }

    /// <summary>
    /// Reclaims one extra slot this system previously freed only after the ID console confirms that
    /// it actually assigned that job. A generic <c>RecordModifiedEvent</c> cannot be used here: it is
    /// also raised for name, personnel-status and criminal-record edits and would silently consume a
    /// real vacancy without anyone taking the job.
    /// </summary>
    private void OnJobAssigned(IdCardJobAssignedEvent ev)
    {
        if (_station.GetOwningStation(ev.TargetId) is not { } station)
            return;

        if (!TryComp<PersonnelSlotTrackingComponent>(station, out var tracking))
            return;

        if (!tracking.ExtraSlotsFreed.TryGetValue(ev.NewJob, out var extra) || extra <= 0)
            return;

        if (!_stationJobs.TryGetJobSlot(station, ev.NewJob.Id, out var freeSlots) || freeSlots is not { } free || free <= 0)
            return;

        if (!_stationJobs.TryAdjustJobSlot(station, ev.NewJob.Id, -1, createSlot: false, clamp: true))
            return;

        tracking.ExtraSlotsFreed[ev.NewJob] = extra - 1;
    }

    private void OnOrderExecuted(ref PersonnelOrderExecutedEvent ev)
    {
        var station = ev.Key.OriginStation;

        // Every console instance on a station shares the same yaml-defined config - see the
        // identical helper/comment in PersonnelOrderCompletionSystem. Absent any console at all,
        // fall back to the safe defaults (free on both demotion and dismissal, nothing excluded)
        // rather than silently doing nothing.
        TryFindStationConsole(station, out var console);

        if (ev.PreviousStatus == EmploymentStatus.Demotion
            && console is { } demotionConsole
            && !_console.GetFreeSlotOnDemotion(demotionConsole.Comp))
        {
            return;
        }

        if (console is { } found && IsExcluded(found.Comp, ev.ExecutedJob))
            return;

        TryFreeSlot(station, ev.Key, ev.ExecutedJob);
    }

    private bool IsExcluded(SharedPersonnelRecordsConsoleComponent config, ProtoId<JobPrototype> job)
    {
        if (_console.IsJobExcluded(config, job))
            return true;

        return _jobSystem.TryGetPrimaryDepartment(job.Id, out var dept) && _console.IsDepartmentBlacklisted(config, dept.ID);
    }

    private void TryFreeSlot(EntityUid station, StationRecordKey key, ProtoId<JobPrototype> job)
    {
        if (!_records.TryGetRecord<PersonnelRecord>(key, out var record))
            return;

        // Second line of defense: this exact record can only free a slot for this exact job once
        // per round, regardless of what the occupied/free math below says. Cheap insurance
        // against a counting mistake elsewhere, per §2.4 point 4.
        if (record.SlotsFreedFor.Contains(job))
            return;

        if (_stationJobs.IsJobUnlimited(station, job.Id))
            return;

        if (!_stationJobs.TryGetJobSlot(station, job.Id, out var freeSlots))
            return;

        var roundStart = _stationJobs.GetRoundStartJobs(station);
        if (!roundStart.TryGetValue(job, out var startSlots) || startSlots is not { } start)
            return; // job wasn't part of this station's round-start configuration at all

        var occupied = _records.GetRecordsOfType<GeneralStationRecord>(station)
            .Count(r => r.Item2.JobPrototype == job.Id);

        // The actual invariant: only add a slot back if doing so still keeps
        // occupied + free within what the station started with.
        if (occupied + (freeSlots ?? 0) >= start)
            return;

        if (!_stationJobs.TryAdjustJobSlot(station, job.Id, 1, createSlot: false, clamp: true))
            return;

        record.SlotsFreedFor.Add(job);
        _records.Synchronize(key);

        var tracking = EnsureComp<PersonnelSlotTrackingComponent>(station);
        tracking.ExtraSlotsFreed[job] = tracking.ExtraSlotsFreed.GetValueOrDefault(job) + 1;
    }

    private bool TryFindStationConsole(EntityUid station, out Entity<SharedPersonnelRecordsConsoleComponent>? console)
    {
        var query = EntityQueryEnumerator<SharedPersonnelRecordsConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_station.GetOwningStation(uid) == station)
            {
                console = (uid, comp);
                return true;
            }
        }

        console = null;
        return false;
    }
}
