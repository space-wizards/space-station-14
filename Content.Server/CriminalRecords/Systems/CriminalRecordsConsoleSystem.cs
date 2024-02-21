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
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;

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
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
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

    private void OnFiltersChanged(Entity<CriminalRecordsConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent);
        }
    }

    private void OnChangeStatus(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordChangeStatus msg)
    {
        // prevent malf client violating wanted/reason nullability
        if ((msg.Status == SecurityStatus.Wanted) != (msg.Reason != null))
            return;

        if (!CheckSelected(ent, msg.Session, out var mob, out var key))
            return;

        if (!_stationRecords.TryGetRecord<CriminalRecord>(key.Value, out var record) || record.Status == msg.Status)
            return;

        // validate the reason
        string? reason = null;
        if (msg.Reason != null)
        {
            reason = msg.Reason.Trim();
            if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
                return;
        }

        // when arresting someone add it to history automatically
        // fallback exists if the player was not set to wanted beforehand
        if (msg.Status == SecurityStatus.Detained)
        {
            var oldReason = record.Reason ?? Loc.GetString("criminal-records-console-unspecified-reason");
            var history = Loc.GetString("criminal-records-console-auto-history", ("reason", oldReason));
            _criminalRecords.TryAddHistory(key.Value, history);
        }

        var oldStatus = record.Status;

        // will probably never fail given the checks above
        _criminalRecords.TryChangeStatus(key.Value, msg.Status, msg.Reason);

        var name = RecordName(key.Value);
        var officer = Loc.GetString("criminal-records-console-unknown-officer");
        if (_idCard.TryFindIdCard(mob.Value, out var id) && id.Comp.FullName is {} fullName)
            officer = fullName;

        (string, object)[] args;
        if (reason != null)
            args = new (string, object)[] { ("name", name), ("officer", officer), ("reason", reason) };
        else
            args = new (string, object)[] { ("name", name), ("officer", officer) };

        // figure out which radio message to send depending on transition
        var statusString = (oldStatus, msg.Status) switch
        {
            // going from wanted or detained on the spot
            (_, SecurityStatus.Detained) => "detained",
            // prisoner did their time
            (SecurityStatus.Detained, SecurityStatus.None) => "released",
            // going from wanted to none, must have been a mistake
            (_, SecurityStatus.None) => "not-wanted",
            // going from none or detained, AOS or prisonbreak / lazy secoff never set them to released and they reoffended
            (_, SecurityStatus.Wanted) => "wanted",
            // this is impossible
            _ => "not-wanted"
        };
        _radio.SendRadioMessage(ent, Loc.GetString($"criminal-records-console-{statusString}", args), ent.Comp.SecurityChannel, ent);

        UpdateUserInterface(ent);
    }

    private void OnAddHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordAddHistory msg)
    {
        if (!CheckSelected(ent, msg.Session, out _, out var key))
            return;

        var line = msg.Line.Trim();
        if (line.Length < 1 || line.Length > ent.Comp.MaxStringLength)
            return;

        if (!_criminalRecords.TryAddHistory(key.Value, line))
            return;

        // no radio message since its not crucial to officers patrolling

        UpdateUserInterface(ent);
    }

    private void OnDeleteHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordDeleteHistory msg)
    {
        if (!CheckSelected(ent, msg.Session, out _, out var key))
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
            _ui.TrySetUiState(uid, CriminalRecordsConsoleKey.Key, new CriminalRecordsConsoleState());
            return;
        }

        var listing = _stationRecords.BuildListing((owningStation.Value, stationRecords), console.Filter);

        var state = new CriminalRecordsConsoleState(listing, console.Filter);
        if (console.ActiveKey is {} id)
        {
            // get records to display when a crewmember is selected
            var key = new StationRecordKey(id, owningStation.Value);
            _stationRecords.TryGetRecord(key, out state.StationRecord, stationRecords);
            _stationRecords.TryGetRecord(key, out state.CriminalRecord, stationRecords);
            state.SelectedKey = id;
        }

        _ui.TrySetUiState(uid, CriminalRecordsConsoleKey.Key, state);
    }

    /// <summary>
    /// Boilerplate that most actions use, if they require that a record be selected.
    /// Obviously shouldn't be used for selecting records.
    /// </summary>
    private bool CheckSelected(Entity<CriminalRecordsConsoleComponent> ent, ICommonSession session,
        [NotNullWhen(true)] out EntityUid? mob, [NotNullWhen(true)] out StationRecordKey? key)
    {
        key = null;
        mob = null;
        if (session.AttachedEntity is not {} user)
            return false;

        if (!_access.IsAllowed(user, ent))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-permission-denied"), ent, session);
            return false;
        }

        if (ent.Comp.ActiveKey is not {} id)
            return false;

        // checking the console's station since the user might be off-grid using on-grid console
        if (_station.GetOwningStation(ent) is not {} station)
            return false;

        key = new StationRecordKey(id, station);
        mob = user;
        return true;
    }

    /// <summary>
    /// Gets the name from a record, or empty string if this somehow fails.
    /// </summary>
    private string RecordName(StationRecordKey key)
    {
        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record))
            return "";

        return record.Name;
    }
}
