using System.Linq;
using Content.Server.CriminalRecords.Components;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.CriminalRecords.Systems;

public sealed class GeneralCriminalRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecordsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, SelectGeneralCriminalRecord>(OnKeySelected);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, GeneralStationRecordsFilterMsg>(OnFiltersChanged);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, CriminalRecordArrestButtonPressed>(OnButtonPressed);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, CriminalStatusOptionButtonSelected>(OnStatusSelected);
    }

    private void UpdateUserInterface<T>(EntityUid uid, GeneralCriminalRecordConsoleComponent component, T ev)
    {
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, GeneralCriminalRecordConsoleComponent component, BoundUIOpenedEvent ev)
    {
        if (ev.Session.AttachedEntity is { Valid: true } ent)
            component.HasAccess = CanUse(ent, uid);

        UpdateUserInterface(uid, component);
    }

    private void OnKeySelected(EntityUid uid, GeneralCriminalRecordConsoleComponent component,
        SelectGeneralCriminalRecord msg)
    {
        component.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(uid, component);
    }

    private void SendRadioMessage(EntityUid sender, string message, string channel)
    {
        _radioSystem.SendRadioMessage(sender, message,
            _prototypeManager.Index<RadioChannelPrototype>(channel), sender);
    }

    private void OnButtonPressed(EntityUid uid, GeneralCriminalRecordConsoleComponent component,
        CriminalRecordArrestButtonPressed msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob)
            return;

        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("general-criminal-record-permission-denied"), uid, msg.Session);
            return;
        }

        var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

        if (!_criminalRecordsSystem.TryArrest(station!.Value, component.ActiveKey!.Value, out var status, msg.Reason))
            return;

        (string, object)[] args =
        {
            ("name", msg.Name), ("reason", msg.Reason),
            ("officer", Name(msg.Session.AttachedEntity.Value)), ("hasReason", msg.Reason.Length)
        };

        // Using dictionary because switch-statements don't seem to work here for some reason
        var messages = new Dictionary<SecurityStatus, string>
        {
            { SecurityStatus.Detained, "general-criminal-record-console-detained" },
            { SecurityStatus.None, "general-criminal-record-console-released" }
        };
        SendRadioMessage(uid, Loc.GetString(messages[status.Value], args), component.SecurityChannel);

        UpdateUserInterface(uid, component);
    }

    private void OnStatusSelected(EntityUid uid, GeneralCriminalRecordConsoleComponent component,
        CriminalStatusOptionButtonSelected msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob)
            return;

        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("general-criminal-record-permission-denied"), uid, msg.Session);
            return;
        }

        var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

        if (!_criminalRecordsSystem.TryChangeStatus(station!.Value, component.ActiveKey!.Value, msg.Status,
                out var status, msg.Reason))
            return;

        (string, object)[] args =
        {
            ("name", msg.Name), ("reason", msg.Reason),
            ("officer", Name(msg.Session.AttachedEntity.Value)), ("hasReason", msg.Reason.Length)
        };

        // Using dictionary because switch-statements don't seem to work here for some reason
        var messages = new Dictionary<SecurityStatus, string>
        {
            { SecurityStatus.Wanted, "general-criminal-record-console-wanted"},
            { SecurityStatus.None, "general-criminal-record-console-not-wanted"}
        };
        SendRadioMessage(uid, Loc.GetString(messages[status.Value], args), component.SecurityChannel);

        UpdateUserInterface(uid, component);
    }

    private bool CanUse(EntityUid user, EntityUid console)
    {
        if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent))
            return _accessReaderSystem.IsAllowed(user, accessReaderComponent);
        return true;
    }

    private void UpdateUserInterface(EntityUid uid, GeneralCriminalRecordConsoleComponent? console = null)
    {
        if (!Resolve(uid, ref console))
        {
            return;
        }

        var owningStation = _stationSystem.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecordsComponent))
        {
            GeneralCriminalRecordConsoleState state = new(null, null, null, null, null);
            SetStateForInterface(uid, state);
            return;
        }

        var consoleRecords =
            _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecordsComponent);

        var listing = new Dictionary<StationRecordKey, string>();
        foreach (var pair in consoleRecords)
        {
            if (console != null && console.Filter != null
                                && IsSkippedRecord(console.Filter, pair.Item2))
            {
                continue;
            }

            listing.Add(pair.Item1, pair.Item2.Name);
        }

        switch (listing.Count)
        {
            case 0:
                GeneralCriminalRecordConsoleState state = new(null, null, null, null, console?.Filter);
                SetStateForInterface(uid, state);
                return;
            case 1:
                console!.ActiveKey = listing.Keys.First();
                break;
        }

        GeneralStationRecord? stationRecord;
        _stationRecordsSystem.TryGetRecord(owningStation.Value, console!.ActiveKey!.Value, out stationRecord,
                stationRecordsComponent);

        GeneralCriminalRecord? criminalRecord;
        _stationRecordsSystem.TryGetRecord(owningStation.Value, console.ActiveKey.Value, out criminalRecord,
                stationRecordsComponent);

        GeneralCriminalRecordConsoleState newState = new(console.ActiveKey, stationRecord, criminalRecord, listing, console.Filter);
        SetStateForInterface(uid, newState);
    }

    private void SetStateForInterface(EntityUid uid, GeneralCriminalRecordConsoleState newState)
    {
        _userInterface.TrySetUiState(uid, GeneralCriminalRecordConsoleKey.Key, newState);
    }

    #region Filters
    private void OnFiltersChanged(EntityUid uid,
        GeneralCriminalRecordConsoleComponent component, GeneralStationRecordsFilterMsg msg)
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
