using Content.Server.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Server.Security.Components;
using Content.Shared.Database;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.StationRecords.Systems;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, SelectGeneralStationRecord>(OnKeySelected);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, GeneralStationRecordsFilterMsg>(OnFiltersChanged);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, StationRecordArrestButtonPressed>(OnButtonPressed);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, StatusOptionButtonSelected>(OnStatusSelected);
    }

    private void UpdateUserInterface<T>(EntityUid uid, GeneralStationRecordConsoleComponent component, T ev)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnKeySelected(EntityUid uid, GeneralStationRecordConsoleComponent component,
        SelectGeneralStationRecord msg)
    {
        component.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(uid, component);
    }

    private void OnButtonPressed(EntityUid uid, GeneralStationRecordConsoleComponent component,
        StationRecordArrestButtonPressed msg)
    {
        TryComp<SecurityInfoComponent>(msg.Session.AttachedEntity, out var secInfo);

        if (msg.Reason != string.Empty && msg.Name != null && msg.Session.AttachedEntity != null && secInfo != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.Detained ? SecurityStatus.None : SecurityStatus.Detained;

            var message = secInfo.Status switch
            {
                SecurityStatus.Detained => $"{msg.Name} has been detained for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}",
                SecurityStatus.None => $"{msg.Name} has been released from the detention for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}"
            };

            _radioSystem.SendRadioMessage(uid, message, _prototypeManager.Index<RadioChannelPrototype>("Security"));

            if (secInfo.Status == SecurityStatus.Detained)
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} has been detained for {msg.Reason} by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }
            else
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} has been released from the detention for {msg.Reason} by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }

            UpdateUserInterface(uid, component);
        }

        else if (msg.Reason == string.Empty && msg.Name != null && msg.Session.AttachedEntity != null && secInfo != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.Detained ? SecurityStatus.None : SecurityStatus.Detained;

            var message = secInfo.Status switch
            {
                SecurityStatus.Detained => $"{msg.Name} has been detained for by {Name(msg.Session.AttachedEntity.Value)}",
                SecurityStatus.None => $"{msg.Name} has been released from the detention by {Name(msg.Session.AttachedEntity.Value)}"
            };

            _radioSystem.SendRadioMessage(uid, message, _prototypeManager.Index<RadioChannelPrototype>("Security"));

            if (secInfo.Status == SecurityStatus.Detained)
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} has been detained by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }
            else
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} has been released from the detention by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }

            UpdateUserInterface(uid, component);
        }

        var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);
        _stationRecordsSystem.Synchronize(station!.Value);
        UpdateUserInterface(uid, component);
    }

    private void OnStatusSelected(EntityUid uid, GeneralStationRecordConsoleComponent component,
        StatusOptionButtonSelected msg)
    {
        TryComp<SecurityInfoComponent>(msg.Session.AttachedEntity, out var secInfo);

        if (msg.Reason != string.Empty && secInfo != null && msg.Session.AttachedEntity != null)
        {
            if (secInfo.Status == msg.Status)
                return;

            secInfo.Status = secInfo.Status == SecurityStatus.None ? SecurityStatus.Wanted : SecurityStatus.None;

            var message = secInfo.Status switch
            {
                SecurityStatus.Wanted => $"{msg.Name} is wanted for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}",
                SecurityStatus.None => $"{msg.Name} is not wanted anymore for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}"
            };

            _radioSystem.SendRadioMessage(uid, message, _prototypeManager.Index<RadioChannelPrototype>("Security"));

            var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);
            _stationRecordsSystem.Synchronize(station!.Value);

            if (secInfo.Status == SecurityStatus.Wanted)
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} is wanted for {msg.Reason} by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }
            else
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} is not wanted anymore for {msg.Reason} by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }

            UpdateUserInterface(uid, component);
        }

        else if (msg.Reason == string.Empty && secInfo != null && msg.Session.AttachedEntity != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.None ? SecurityStatus.Wanted : SecurityStatus.None;

            var message = secInfo.Status switch
            {
                SecurityStatus.Wanted => $"{msg.Name} is wanted by {Name(msg.Session.AttachedEntity.Value)}",
                SecurityStatus.None => $"{msg.Name} is not wanted anymore by {Name(msg.Session.AttachedEntity.Value)}"
            };

            _radioSystem.SendRadioMessage(uid, message, _prototypeManager.Index<RadioChannelPrototype>("Security"));
            var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);
            _stationRecordsSystem.Synchronize(station!.Value);

            if (secInfo.Status == SecurityStatus.Wanted)
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} is wanted by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }
            else
            {
                _adminLogger.Add(LogType.CriminalRecords, LogImpact.Medium,
                    $"{msg.Name} is not wanted anymore by {ToPrettyString(msg.Session.AttachedEntity.Value):msg.Session.AttachedEntity.Value}");
            }

            UpdateUserInterface(uid, component);
        }
    }

    private async void UpdateUserInterface(EntityUid uid, GeneralStationRecordConsoleComponent? console = null)
    private void OnFiltersChanged(EntityUid uid,
        GeneralStationRecordConsoleComponent component, GeneralStationRecordsFilterMsg msg)
    {
        if (component.Filter == null ||
            component.Filter.Type != msg.Type || component.Filter.Value != msg.Value)
        {
            component.Filter = new GeneralStationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(uid, component);
        }
    }

    private void UpdateUserInterface(EntityUid uid,
        GeneralStationRecordConsoleComponent? console = null)
    {
        if (!Resolve(uid, ref console))
        {
            return;
        }

        var owningStation = _stationSystem.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecordsComponent))
        {
            GeneralStationRecordConsoleState state = new(null, null, null, null);
            SetStateForInterface(uid, state);
            return;
        }

        var consoleRecords =
            _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecordsComponent);

        var listing = new Dictionary<StationRecordKey, string>();

        foreach (var pair in consoleRecords)
        {
            if (console.Filter != null && IsSkippedRecord(console.Filter, pair.Item2))
            {
                continue;
            }

            listing.Add(pair.Item1, pair.Item2.Name);
        }

        if (listing.Count == 0)
        {
            GeneralStationRecordConsoleState state = new(null, null, null, console.Filter);
            SetStateForInterface(uid, state);
            return;
        }
        else if (listing.Count == 1)
        {
            console.ActiveKey = listing.Keys.First();
        }

        GeneralStationRecord? record = null;
        if (console.ActiveKey != null)
        {
            _stationRecordsSystem.TryGetRecord(owningStation.Value, console.ActiveKey.Value, out record,
                stationRecordsComponent);
        }

        GeneralStationRecordConsoleState newState = new(console.ActiveKey, record, listing, console.Filter);
        SetStateForInterface(uid, newState);
    }

    private void SetStateForInterface(EntityUid uid, GeneralStationRecordConsoleState newState)
    {
        _userInterface
            .GetUiOrNull(uid, GeneralStationRecordConsoleKey.Key)
            ?.SetState(newState);
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
