using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.IdentityManagement;
using Content.Shared.Security.Components;
using System.Linq;
using Content.Shared.Roles.Jobs;

namespace Content.Server.CriminalRecords.Systems;

/// <summary>
/// Handles all UI for criminal records console
/// </summary>
public sealed class CriminalRecordsConsoleSystem : SharedCriminalRecordsConsoleSystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);

        Subs.BuiEvents<CriminalRecordsConsoleComponent>(CriminalRecordsConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
            subs.Event<CriminalRecordChangeStatus>(OnChangeStatus);
            subs.Event<CriminalRecordAddHistory>(OnAddHistory);
            subs.Event<CriminalRecordDeleteHistory>(OnDeleteHistory);
            subs.Event<CriminalRecordSetStatusFilter>(OnStatusFilterPressed);
        });
    }

    private void UpdateUserInterface<T>(Entity<CriminalRecordsConsoleComponent> ent, ref T args)
    {
        // TODO: this is probably wasteful, maybe better to send a message to modify the exact state?
        UpdateUserInterface(ent);
    }

    private void OnKeySelected(Entity<CriminalRecordsConsoleComponent> ent, ref SelectStationRecord msg)
    {
        // no concern of sus client since record retrieval will fail if invalid id is given
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
    }
    private void OnStatusFilterPressed(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordSetStatusFilter msg)
    {
        ent.Comp.FilterStatus = msg.FilterStatus;
        UpdateUserInterface(ent);
    }

    private void OnFiltersChanged(Entity<CriminalRecordsConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent);
        }
    }

    private void GetOfficer(EntityUid uid, out string officer)
    {
        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(null, uid);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);
        officer = tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("criminal-records-console-unknown-officer");
    }

    private void OnChangeStatus(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordChangeStatus msg)
    {
        // prevent malf client violating wanted/reason nullability
        if (msg.Status == SecurityStatus.Wanted != (msg.Reason != null) &&
            msg.Status == SecurityStatus.Suspected != (msg.Reason != null))
            return;

        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        if (!_records.TryGetRecord<CriminalRecord>(key.Value, out var record) || record.Status == msg.Status)
            return;

        // validate the reason
        string? reason = null;
        if (msg.Reason != null)
        {
            reason = msg.Reason.Trim();
            if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
                return;
        }

        var oldStatus = record.Status;

        var name = _records.RecordName(key.Value);
        GetOfficer(mob.Value, out var officer);

        // when arresting someone add it to history automatically
        // fallback exists if the player was not set to wanted beforehand
        if (msg.Status == SecurityStatus.Detained)
        {
            var oldReason = record.Reason ?? Loc.GetString("criminal-records-console-unspecified-reason");
            var history = Loc.GetString("criminal-records-console-auto-history", ("reason", oldReason));
            _criminalRecords.TryAddHistory(key.Value, history, officer);
        }

        // will probably never fail given the checks above
        name = _records.RecordName(key.Value);
        officer = Loc.GetString("criminal-records-console-unknown-officer");
        var jobName = "Unknown";

        _records.TryGetRecord<GeneralStationRecord>(key.Value, out var entry);
        if (entry != null)
            jobName = entry.JobTitle;

        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(null, mob.Value);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);
        if (tryGetIdentityShortInfoEvent.Title != null)
            officer = tryGetIdentityShortInfoEvent.Title;

        _criminalRecords.TryChangeStatus(key.Value, msg.Status, msg.Reason, officer);

        (string, object)[] args;
        if (reason != null)
            args = new (string, object)[] { ("name", name), ("officer", officer), ("reason", reason), ("job", jobName) };
        else
            args = new (string, object)[] { ("name", name), ("officer", officer), ("job", jobName) };

        // figure out which radio message to send depending on transition
        var statusString = (oldStatus, msg.Status) switch
        {
            // person has been detained
            (_, SecurityStatus.Detained) => "detained",
            // person did something sus
            (_, SecurityStatus.Suspected) => "suspected",
            // released on parole
            (_, SecurityStatus.Paroled) => "paroled",
            // prisoner did their time
            (_, SecurityStatus.Discharged) => "released",
            // going from any other state to wanted, AOS or prisonbreak / lazy secoff never set them to released and they reoffended
            (_, SecurityStatus.Wanted) => "wanted",
            // person is no longer sus
            (SecurityStatus.Suspected, SecurityStatus.None) => "not-suspected",
            // going from wanted to none, must have been a mistake
            (SecurityStatus.Wanted, SecurityStatus.None) => "not-wanted",
            // criminal status removed
            (SecurityStatus.Detained, SecurityStatus.None) => "released",
            // criminal is no longer on parole
            (SecurityStatus.Paroled, SecurityStatus.None) => "not-parole",
            // this is impossible
            _ => "not-wanted"
        };
        _radio.SendRadioMessage(ent, Loc.GetString($"criminal-records-console-{statusString}", args),
            ent.Comp.SecurityChannel, ent);

        UpdateUserInterface(ent);
    }

    private void OnAddHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordAddHistory msg)
    {
        if (!CheckSelected(ent, msg.Actor, out var mob, out var key))
            return;

        var line = msg.Line.Trim();
        if (line.Length < 1 || line.Length > ent.Comp.MaxStringLength)
            return;

        GetOfficer(mob.Value, out var officer);

        if (!_criminalRecords.TryAddHistory(key.Value, line, officer))
            return;

        // no radio message since its not crucial to officers patrolling

        UpdateUserInterface(ent);
    }

    private void OnDeleteHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordDeleteHistory msg)
    {
        if (!CheckSelected(ent, msg.Actor, out _, out var key))
            return;

        if (!_criminalRecords.TryDeleteHistory(key.Value, msg.Index))
            return;

        // a bit sus but not crucial to officers patrolling

        UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<CriminalRecordsConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            _ui.SetUiState(uid, CriminalRecordsConsoleKey.Key, new CriminalRecordsConsoleState());
            return;
        }

        // get the listing of records to display
        var listing = _records.BuildListing((owningStation.Value, stationRecords), console.Filter);

        // filter the listing by the selected criminal record status
        //if NONE, dont filter by status, just show all crew
        if (console.FilterStatus != SecurityStatus.None)
        {
            listing = listing
                .Where(x => _records.TryGetRecord<CriminalRecord>(new StationRecordKey(x.Key, owningStation.Value), out var record) && record.Status == console.FilterStatus)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        var state = new CriminalRecordsConsoleState(listing, console.Filter);
        if (console.ActiveKey is { } id)
        {
            // get records to display when a crewmember is selected
            var key = new StationRecordKey(id, owningStation.Value);
            _records.TryGetRecord(key, out state.StationRecord, stationRecords);
            _records.TryGetRecord(key, out state.CriminalRecord, stationRecords);
            state.SelectedKey = id;
        }

        // Set the Current Tab aka the filter status type for the records list
        state.FilterStatus = console.FilterStatus;

        _ui.SetUiState(uid, CriminalRecordsConsoleKey.Key, state);
    }

    /// <summary>
    /// Boilerplate that most actions use, if they require that a record be selected.
    /// Obviously shouldn't be used for selecting records.
    /// </summary>
    private bool CheckSelected(Entity<CriminalRecordsConsoleComponent> ent, EntityUid user,
        [NotNullWhen(true)] out EntityUid? mob, [NotNullWhen(true)] out StationRecordKey? key)
    {
        key = null;
        mob = null;

        if (!_access.IsAllowed(user, ent))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-permission-denied"), ent, user);
            return false;
        }

        if (ent.Comp.ActiveKey is not { } id)
            return false;

        // checking the console's station since the user might be off-grid using on-grid console
        if (_station.GetOwningStation(ent) is not { } station)
            return false;

        key = new StationRecordKey(id, station);
        mob = user;
        return true;
    }

    /// <summary>
    /// Checks if the new identity's name has a criminal record attached to it, and gives the entity the icon that
    /// belongs to the status if it does.
    /// </summary>
    public void CheckNewIdentity(EntityUid uid)
    {
        var name = Identity.Name(uid, EntityManager);
        var xform = Transform(uid);

        // TODO use the entity's station? Not the station of the map that it happens to currently be on?
        var station = _station.GetStationInMap(xform.MapID);

        if (station != null && _records.GetRecordByName(station.Value, name) is { } id)
        {
            if (_records.TryGetRecord<CriminalRecord>(new StationRecordKey(id, station.Value),
                    out var record))
            {
                if (record.Status != SecurityStatus.None)
                {
                    _criminalRecords.SetCriminalIcon(name, record.Status, uid);
                    return;
                }
            }
        }
        RemComp<CriminalRecordComponent>(uid);
    }
}
