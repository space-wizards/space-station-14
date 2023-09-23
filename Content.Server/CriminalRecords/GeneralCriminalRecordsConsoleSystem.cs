using System.Linq;
using Content.Server.CriminalRecords.Components;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Shared.Security.Components;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Emag.Components;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.CriminalRecords.Systems;

public sealed class GeneralCriminalRecordsConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, SelectGeneralCriminalRecord>(OnKeySelected);
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, GeneralStationRecordsFilterMsg>(OnFiltersChanged);
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, CriminalRecordArrestButtonPressed>(OnArrestButtonPressed);
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, CriminalRecordReleaseButtonPressed>(OnReleaseButtonPressed);
        SubscribeLocalEvent<GeneralCriminalRecordsConsoleComponent, CriminalRecordWantedButtonPressed>(OnWantedButtonPressed);
    }

    private void OnFiltersChanged(EntityUid uid,
        GeneralCriminalRecordsConsoleComponent component, GeneralStationRecordsFilterMsg msg)
    {
        if (component.Filter == null ||
            component.Filter.Type != msg.Type || component.Filter.Value != msg.Value)
        {
            component.Filter = new GeneralStationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(uid, component);
        }
    }

    private void UpdateUserInterface<T>(EntityUid uid, GeneralCriminalRecordsConsoleComponent component, T ev)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnKeySelected(EntityUid uid, GeneralCriminalRecordsConsoleComponent component,
        SelectGeneralCriminalRecord msg)
    {
        component.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(uid, component);
    }

    private void OnArrestButtonPressed(EntityUid uid, GeneralCriminalRecordsConsoleComponent component,
        CriminalRecordArrestButtonPressed msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("general-criminal-records-permission-denied"), uid, msg.Session);
            return;
        }
		
		var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

        TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

        _stationRecordsSystem.TryGetRecord(station!.Value, _stationRecordsSystem.Convert(component.ActiveKey!.Value), out GeneralCriminalRecord? record, stationRecordsComponent);

        if (msg.Reason != string.Empty && msg.Reason != null && msg.Name != null && msg.Session.AttachedEntity != null && record != null && record.Status != null)
        {
            record.Reason = msg.Reason;
			record.Status = SecurityStatus.Detained;

            _radioSystem.SendRadioMessage(uid, Loc.GetString("general-criminal-records-console-detained-with-reason", ("name", msg.Name), ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))), _prototypeManager.Index<RadioChannelPrototype>("Security"), uid);
        }

        else if (msg.Reason == string.Empty && msg.Reason != null && msg.Name != null && msg.Session.AttachedEntity != null && record != null && record.Status != null)
        {
            record.Reason = string.Empty;
			record.Status = SecurityStatus.Detained;

            _radioSystem.SendRadioMessage(uid, Loc.GetString("general-criminal-records-console-detained-without-reason", ("name", msg.Name), ("goodguyname", Name(msg.Session.AttachedEntity.Value))), _prototypeManager.Index<RadioChannelPrototype>("Security"), uid);
        }
		_stationRecordsSystem.Synchronize(station.Value);
        UpdateUserInterface(uid, component);
    }
	
	private void OnWantedButtonPressed(EntityUid uid, GeneralCriminalRecordsConsoleComponent component,
        CriminalRecordWantedButtonPressed msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("general-criminal-records-permission-denied"), uid, msg.Session);
            return;
        }
		
		var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

        TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

        _stationRecordsSystem.TryGetRecord(station!.Value, _stationRecordsSystem.Convert(component.ActiveKey!.Value), out GeneralCriminalRecord? record, stationRecordsComponent);

        if (msg.Reason != string.Empty && msg.Reason != null && msg.Name != null && msg.Session.AttachedEntity != null && record != null && record.Status != null)
        {
            record.Reason = msg.Reason;
			record.Status = SecurityStatus.Wanted;

            _radioSystem.SendRadioMessage(uid, Loc.GetString("general-criminal-records-console-wanted-with-reason", ("name", msg.Name), ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))), _prototypeManager.Index<RadioChannelPrototype>("Security"), uid);
        }

        else if (msg.Reason == string.Empty && msg.Reason != null && msg.Name != null && msg.Session.AttachedEntity != null && record != null && record.Status != null)
        {
            record.Reason = string.Empty;
			record.Status = SecurityStatus.Wanted;

            _radioSystem.SendRadioMessage(uid, Loc.GetString("general-criminal-records-console-wanted-without-reason", ("name", msg.Name), ("goodguyname", Name(msg.Session.AttachedEntity.Value))), _prototypeManager.Index<RadioChannelPrototype>("Security"), uid);
        }
		_stationRecordsSystem.Synchronize(station.Value);
        UpdateUserInterface(uid, component);
    }
	
	private void OnReleaseButtonPressed(EntityUid uid, GeneralCriminalRecordsConsoleComponent component,
        CriminalRecordReleaseButtonPressed msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("general-criminal-records-permission-denied"), uid, msg.Session);
            return;
        }

        var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

        TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);
		
        _stationRecordsSystem.TryGetRecord(station!.Value, _stationRecordsSystem.Convert(component.ActiveKey!.Value), out GeneralCriminalRecord? record, stationRecordsComponent);

        if (msg.Reason != string.Empty && msg.Reason != null && msg.Name != null && msg.Session.AttachedEntity != null && record != null && record.Status != null)
        {

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Detained, Loc.GetString("general-criminal-records-console-undetained-with-reason", ("name", msg.Name), ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) },
                { SecurityStatus.Wanted, Loc.GetString("general-criminal-records-console-not-wanted-with-reason", ("name", msg.Name), ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) }
            };

            _radioSystem.SendRadioMessage(uid, messages[record.Status.Value], _prototypeManager.Index<RadioChannelPrototype>("Security"), uid);
			
			record.Reason = msg.Reason;
			record.Status = SecurityStatus.None;
        }

        else if (msg.Reason == string.Empty && msg.Reason != null && msg.Name != null && msg.Session.AttachedEntity != null && record != null && record.Status != null)
        {

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Detained, Loc.GetString("general-criminal-records-console-undetained-without-reason", ("name", msg.Name), ("goodguyname", Name(msg.Session.AttachedEntity.Value))) },
                { SecurityStatus.Wanted, Loc.GetString("general-criminal-records-console-not-wanted-without-reason", ("name", msg.Name), ("goodguyname", Name(msg.Session.AttachedEntity.Value))) }
            };

            _radioSystem.SendRadioMessage(uid, messages[record.Status.Value], _prototypeManager.Index<RadioChannelPrototype>("Security"), uid);
			
			record.Reason = string.Empty;
			record.Status = SecurityStatus.None;
        } else return;
		
		
		_stationRecordsSystem.Synchronize(station.Value);
        UpdateUserInterface(uid, component);
    }
	
	private bool CanUse(EntityUid user, EntityUid console)
    {
        if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent) && !HasComp<EmaggedComponent>(console))
        {
            return _accessReaderSystem.IsAllowed(user, console);
        }
        return true;
    }

    private void UpdateUserInterface(EntityUid uid, GeneralCriminalRecordsConsoleComponent? console = null)
    {
        if (!Resolve(uid, ref console))
        {
            return;
        }

        var owningStation = _stationSystem.GetOwningStation(uid);
		
		

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecordsComponent))
        {
            GeneralCriminalRecordsConsoleState state = new(null, null, null, null, null);
            SetStateForInterface(uid, state);
            return;
        }

        var consoleRecords =
            _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecordsComponent);

        var listing = new Dictionary<(NetEntity, uint), string>();
        foreach (var pair in consoleRecords)
        {
            if (console != null && console.Filter != null
                                && IsSkippedRecord(console.Filter, pair.Item2))
            {
                continue;
            }

            listing.Add(_stationRecordsSystem.Convert(pair.Item1), pair.Item2.Name);
        }

        if (listing.Count == 0)
        {
            GeneralCriminalRecordsConsoleState state = new(null, null, null, null, console?.Filter);
            SetStateForInterface(uid, state);
            return;
        }
        else if (listing.Count == 1)
        {
            console!.ActiveKey = listing.Keys.First();
        }

        GeneralStationRecord? stationRecord = null;
        if (console!.ActiveKey != null)
        {
            _stationRecordsSystem.TryGetRecord(owningStation.Value, _stationRecordsSystem.Convert(console.ActiveKey.Value), out stationRecord,
                stationRecordsComponent);
        }

        GeneralCriminalRecord? criminalRecord = null;
        if (console.ActiveKey != null)
        {
            _stationRecordsSystem.TryGetRecord(owningStation.Value, _stationRecordsSystem.Convert(console.ActiveKey.Value), out criminalRecord,
                stationRecordsComponent);
        }

        GeneralCriminalRecordsConsoleState newState = new(console.ActiveKey, stationRecord, criminalRecord, listing, console.Filter);
        SetStateForInterface(uid, newState);
    }

    private void SetStateForInterface(EntityUid uid, GeneralCriminalRecordsConsoleState newState)
    {
        _userInterface.TrySetUiState(uid, GeneralCriminalRecordsConsoleKey.Key, newState);
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
}