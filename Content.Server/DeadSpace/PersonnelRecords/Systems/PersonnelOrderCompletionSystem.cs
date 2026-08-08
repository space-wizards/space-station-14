// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Detects when an active Demotion/Dismissal order has been carried out - the target's job
/// prototype no longer matches <see cref="PersonnelRecord.JobAtOrder"/>. This is the *only* way
/// an order closes: not the ID card console's dismiss button (that's just a convenient shortcut
/// that happens to change the job the normal way), not annul, nothing else. Any way the job could
/// change - the dismiss button, a manual promotion/demotion via the ID card console, an admin
/// command, CentCom - is picked up here identically, because all of them go through
/// <c>StationRecordsSystem.Synchronize</c> and raise <see cref="RecordModifiedEvent"/>.
/// </summary>
public sealed class PersonnelOrderCompletionSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PersonnelRecordsConsoleSystem _console = default!;
    [Dependency] private readonly PersonnelRecordsSystem _personnelRecords = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecordModifiedEvent>(OnRecordModified);
    }

    private void OnRecordModified(RecordModifiedEvent ev)
    {
        if (!_records.TryGetRecord<PersonnelRecord>(ev.Key, out var record))
            return;

        if (record.Status is not (EmploymentStatus.Demotion or EmploymentStatus.Dismissal))
            return;

        if (record.JobAtOrder is not { } jobAtOrder)
            return;

        if (!_records.TryGetRecord<GeneralStationRecord>(ev.Key, out var general))
            return;

        // Job hasn't actually changed yet - the order is still just sitting there pending.
        if (general.JobPrototype == jobAtOrder.Id)
            return;

        if (!_personnelRecords.TryExecuteOrder(ev.Key))
            return;

        _adminLogger.Add(LogType.Identity, LogImpact.High,
            $"Personnel order against {general.Name} ({ev.Key.Id}) was executed: job changed from {jobAtOrder.Id} to {general.JobPrototype}");

        AnnounceExecuted(ev.Key.OriginStation, general);
    }

    /// <summary>
    /// Purely a system notice - no officer/reason attached, since execution is detected
    /// passively and isn't itself an action anyone "took" in the narrative sense (whatever
    /// changed the job already got its own announcement when the order was issued).
    /// Security-only by explicit request: unlike the issue/annul announcements, this one does not
    /// also go to the target's department channel - the department already got its own notice when
    /// the order was issued, and Security is the only audience left that still needs to be told
    /// the case is closed.
    /// Uses whichever Personnel Records console happens to exist on the station for its Security
    /// channel configuration, same as <c>PersonnelVacancySystem</c>; if none exists (e.g.
    /// destroyed), the announcement is silently skipped rather than guessing at a default channel.
    /// </summary>
    private void AnnounceExecuted(EntityUid station, GeneralStationRecord general)
    {
        if (!TryFindStationConsole(station, out var console))
            return;

        var args = new (string, object)[] { ("name", general.Name) };

        _radio.SendRadioMessage(console, Loc.GetString("personnel-records-console-announce-executed-security", args), _console.GetSecurityChannel(console.Comp), console);
    }

    /// <summary>
    /// There's exactly one console prototype (<c>ComputerPersonnelRecords</c>) and every instance
    /// on a station shares the same yaml-defined channel configuration, so any one of them is
    /// representative - this isn't "the console that issued the order", there may not even be one
    /// still standing.
    /// </summary>
    private bool TryFindStationConsole(EntityUid station, out Entity<PersonnelRecordsConsoleComponent> console)
    {
        var query = EntityQueryEnumerator<PersonnelRecordsConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_station.GetOwningStation(uid) == station)
            {
                console = (uid, comp);
                return true;
            }
        }

        console = default;
        return false;
    }
}
