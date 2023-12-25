using Content.Server.CriminalRecords.Components;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.CriminalRecords.Systems;

public sealed class CriminalRecordsConsoleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, SelectCriminalRecords>(OnKeySelected);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, GeneralStationRecordsFilterMsg>(OnFiltersChanged);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalRecordArrestButtonPressed>(OnButtonPressed);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalStatusOptionButtonSelected>(OnStatusSelected);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalRecordAddHistory>(OnAddHistory);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, CriminalRecordDeleteHistory>(OnDeleteHistory);
    }

    private void UpdateUserInterface<T>(EntityUid uid, CriminalRecordsConsoleComponent component, T ev)
    {
        // TODO: this is probably wasteful, maybe better to send a message to modify the exact state?
        UpdateUserInterface(uid, component);
    }

    private void OnKeySelected(Entity<CriminalRecordsConsoleComponent> ent, ref SelectCriminalRecords msg)
    {
        // no concern of sus client since record retrieval will fail if invalid id is given
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent, ent.Comp);
    }

    private void SendRadioMessage(EntityUid sender, string message, string channel)
    {
        _radio.SendRadioMessage(sender, message,
            _proto.Index<RadioChannelPrototype>(channel), sender);
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
        SendRadioMessage(ent, Loc.GetString($"criminal-records-console-{message}", args), ent.Comp.SecurityChannel);

        UpdateUserInterface(ent, ent.Comp);
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
        SendRadioMessage(ent, Loc.GetString($"criminal-records-console-{message}", args), ent.Comp.SecurityChannel);

        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnAddHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordAddHistory msg)
    {
        if (!CheckSelected(ent, msg.Session, out var mob, out var key))
            return;

        var line = msg.Line.Trim();
        if (string.IsNullorEmpty(line))
            return;

        if (!_criminalRecords.TryAddHistory(key.Value, line))
            return;

        // no radio message since its not crucial to officers patrolling

        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnDeleteHistory(Entity<CriminalRecordsConsoleComponent> ent, ref CriminalRecordDeleteHistory msg)
    {
        if (!CheckSelected(ent, msg.Session, out var mob, out var key))
            return;

        if (!_criminalRecords.TryDeleteHistory(key.Value, msg.Index))
            return;

        // a bit sus but not crucial to officers patrolling

        UpdateUserInterface(ent, ent.Comp);
    }

    private void UpdateUserInterface(EntityUid uid, CriminalRecordsConsoleComponent? console = null)
    {
        if (!Resolve(uid, ref console))
            return;

        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            SetStateForInterface(uid, new CriminalRecordsConsoleState());
            return;
        }

        var consoleRecords =
            _stationRecords.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecords);

        var listing = new Dictionary<uint, string>();
        foreach (var pair in consoleRecords)
        {
            if (console.Filter != null && IsSkippedRecord(console.Filter, pair.Item2))
            {
                continue;
            }

            listing.Add(pair.Item1, pair.Item2.Name);
        }

        // when there is only 1 record automatically select it
        switch (listing.Count)
        {
            case 0:
                SetStateForInterface(uid, new CriminalRecordsConsoleState());
                return;
            case 1:
                console.ActiveKey = listing.Keys.First();
                break;
        }

        // get records to display when a crewmember is selected
        GeneralStationRecord? stationRecord = null;
        CriminalRecord? criminalRecord = null;
        if (console.ActiveKey is {} id)
        {
            Log.Debug($"Selected id is {id}");
            var key = new StationRecordKey(id, owningStation.Value);
            _stationRecords.TryGetRecord<GeneralStationRecord>(key, out stationRecord, stationRecords);
            _stationRecords.TryGetRecord<CriminalRecord>(key, out criminalRecord, stationRecords);
            Log.Debug($"Record {stationRecord} {criminalRecord}");
        }

        CriminalRecordsConsoleState newState = new(console.ActiveKey, stationRecord, criminalRecord, listing, console.Filter);
        SetStateForInterface(uid, newState);
    }

    private void SetStateForInterface(EntityUid uid, CriminalRecordsConsoleState newState)
    {
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

    #region Filters
    private void OnFiltersChanged(EntityUid uid,
        CriminalRecordsConsoleComponent component, GeneralStationRecordsFilterMsg msg)
    {
        if (component.Filter == null ||
            component.Filter.Type != msg.Type || component.Filter.Value != msg.Value)
        {
            component.Filter = new GeneralStationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(uid, component);
        }
    }

    private bool IsSkippedRecord(GeneralStationRecordsFilter filter,
        GeneralStationRecord someRecord)
    {
        bool isFilter = filter.Value.Length > 0;
        string filterLowerCaseValue = "";

        if (!isFilter)
            return false;

        filterLowerCaseValue = filter.Value.ToLower();

        return filter.Type switch
        {
            GeneralStationRecordFilterType.Name =>
                !someRecord.Name.ToLower().Contains(filterLowerCaseValue),
            GeneralStationRecordFilterType.Prints => someRecord.Fingerprint != null
                                                     && IsFilterWithSomeCodeValue(someRecord.Fingerprint, filterLowerCaseValue),
            GeneralStationRecordFilterType.DNA => someRecord.DNA != null
                                                  && IsFilterWithSomeCodeValue(someRecord.DNA, filterLowerCaseValue),
        };
    }

    private bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        return !value.ToLower().StartsWith(filter);
    }
    #endregion
}
