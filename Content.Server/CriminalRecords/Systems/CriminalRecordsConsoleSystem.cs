using System.Linq;
using Content.Server.CriminalRecords.Components;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

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
    }

    private void UpdateUserInterface<T>(EntityUid uid, CriminalRecordsConsoleComponent component, T ev)
    {
        // TODO: this is probably wasteful, maybe better to send a message to modify the exact state?
        UpdateUserInterface(uid, component);
    }

    private void OnKeySelected(EntityUid uid, CriminalRecordsConsoleComponent component,
        SelectCriminalRecords msg)
    {
        component.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(uid, component);
    }

    private void SendRadioMessage(EntityUid sender, string message, string channel)
    {
        _radio.SendRadioMessage(sender, message,
            _proto.Index<RadioChannelPrototype>(channel), sender);
    }

    private void OnButtonPressed(EntityUid uid, CriminalRecordsConsoleComponent component,
        CriminalRecordArrestButtonPressed msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob)
            return;

        if (!_access.IsAllowed(mob, uid))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-permission-denied"), uid, msg.Session);
            return;
        }

        // prevent sus client crashing the server
        if (component.ActiveKey is not {} id)
            return;

        var station = _station.GetOwningStation(mob);
        if (station == null)
            return;

        var key = new StationRecordKey(id, station.Value);
        if (!_criminalRecords.TryArrest(key, out var status, msg.Reason))
            return;

        (string, object)[] args =
        {
            ("name", msg.Name), ("reason", msg.Reason),
            ("officer", Name(mob)), ("hasReason", msg.Reason.Length)
        };

        var message = status == SecurityStatus.Detained ? "detained" : "released";
        SendRadioMessage(uid, Loc.GetString($"criminal-records-console-{message}", args), component.SecurityChannel);

        UpdateUserInterface(uid, component);
    }

    private void OnStatusSelected(EntityUid uid, CriminalRecordsConsoleComponent component,
        CriminalStatusOptionButtonSelected msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob)
            return;

        if (!_access.IsAllowed(mob, uid))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-permission-denied"), uid, msg.Session);
            return;
        }

        // prevent sus client crashing the server
        if (component.ActiveKey is not {} id)
            return;

        var station = _station.GetOwningStation(mob);
        if (station == null)
            return;

        var key = new StationRecordKey(id, station.Value);
        if (!_criminalRecords.TryChangeStatus(key, msg.Status, out var status, msg.Reason))
            return;

        (string, object)[] args =
        {
            ("name", msg.Name), ("reason", msg.Reason),
            ("officer", Name(mob)), ("hasReason", msg.Reason.Length)
        };

        var message = status == SecurityStatus.Wanted ? "wanted" : "not-wanted";
        SendRadioMessage(uid, Loc.GetString($"criminal-records-console-{message}", args), component.SecurityChannel);

        UpdateUserInterface(uid, component);
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
            if (console != null && console.Filter != null
                                && IsSkippedRecord(console.Filter, pair.Item2))
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
