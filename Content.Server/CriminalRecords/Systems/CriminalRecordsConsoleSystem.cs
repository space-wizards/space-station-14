using Content.Server.CriminalRecords.Components;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.CriminalRecords.Systems;

public sealed class CriminalRecordsConsoleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly RecordsConsoleSystem _recordsConsole = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);

        SubscribeLocalEvent<CriminalRecordsConsoleComponent, SelectStationRecord>(OnKeySelected);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, SetStationRecordFilter>(OnFiltersChanged);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalRecordArrestButtonPressed>(OnButtonPressed);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalStatusOptionButtonSelected>(OnStatusSelected);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalRecordAddHistory>(OnAddHistory);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalRecordDeleteHistory>(OnDeleteHistory);
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

    private void OnButtonPressed(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordArrestButtonPressed msg)
    {
        if (!CheckSelected(ent, msg.Session, out var mob, out var key))
            return;

        if (!_criminalRecords.TryArrest(key.Value, out var status, msg.Reason))
            return;

        (string, object)[] args =
        {
            ("name", msg.Name), ("reason", msg.Reason),
            ("officer", Name(mob.Value)), ("hasReason", msg.Reason.Length)
        };

        var message = status == SecurityStatus.Detained ? "detained" : "released";
        _radio.SendRadioMessage(ent, Loc.GetString($"criminal-records-console-{message}", args), ent.Comp.SecurityChannel, ent);

        UpdateUserInterface(ent);
    }

    private void OnStatusSelected(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalStatusOptionButtonSelected msg)
    {
        if (!CheckSelected(ent, msg.Session, out var mob, out var key))
            return;

        if (!_criminalRecords.TryChangeStatus(key.Value, msg.Status, out var status, msg.Reason))
            return;

        (string, object)[] args =
        {
            ("name", msg.Name), ("reason", msg.Reason),
            ("officer", Name(mob.Value)), ("hasReason", msg.Reason.Length)
        };

        var message = status == SecurityStatus.Wanted ? "wanted" : "not-wanted";
        _radio.SendRadioMessage(ent, Loc.GetString($"criminal-records-console-{message}", args), ent.Comp.SecurityChannel, ent);

        UpdateUserInterface(ent);
    }

    private void OnAddHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordAddHistory msg)
    {
        if (!CheckSelected(ent, msg.Session, out _, out var key))
            return;

        var line = msg.Line.Trim();
        if (string.IsNullOrEmpty(line))
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

        var consoleRecords =
            _stationRecords.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecords);

        var listing = new Dictionary<uint, string>();
        foreach (var pair in consoleRecords)
        {
            if (_recordsConsole.IsSkipped(console.Filter, pair.Item2))
                continue;

            listing.Add(pair.Item1, pair.Item2.Name);
        }

        // when there is only 1 record automatically select it
        switch (listing.Count)
        {
            case 0:
                _ui.TrySetUiState(uid, CriminalRecordsConsoleKey.Key, new CriminalRecordsConsoleState());
                return;
            case 1:
                console.ActiveKey = listing.Keys.First();
                break;
        }

        // get records to display when a crewmember is selected
        if (console.ActiveKey is not { } id)
            return;

        Log.Debug($"Selected id is {id}");
        var key = new StationRecordKey(id, owningStation.Value);
        _stationRecords.TryGetRecord<GeneralStationRecord>(key, out var stationRecord, stationRecords);
        _stationRecords.TryGetRecord<CriminalRecord>(key, out var criminalRecord, stationRecords);
        Log.Debug($"Record {stationRecord} {criminalRecord}");

        CriminalRecordsConsoleState newState = new(id, stationRecord, criminalRecord, listing, console.Filter);
        _ui.TrySetUiState(uid, CriminalRecordsConsoleKey.Key, newState);
    }

    /// <summary>
    /// Boilerplate that most buttons use, if they require that a record be selected.
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
}
