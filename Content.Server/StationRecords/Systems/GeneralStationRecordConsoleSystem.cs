using System.Security.AccessControl;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Server.Security.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Serilog;

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
    {
        if (!Resolve(uid, ref console))
        {
            return;
        }

        var owningStation = _stationSystem.GetOwningStation(uid);



        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecordsComponent))
        {
            _userInterface.GetUiOrNull(uid, GeneralStationRecordConsoleKey.Key)?.SetState(new GeneralStationRecordConsoleState(null, null, null));
            return;
        }

        var enumerator = _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecordsComponent);

        var listing = new Dictionary<StationRecordKey, string>();
        foreach (var pair in enumerator)
        {
            listing.Add(pair.Item1, pair.Item2.Name);
        }

        if (listing.Count == 0)
        {
            _userInterface.GetUiOrNull(uid, GeneralStationRecordConsoleKey.Key)?.SetState(new GeneralStationRecordConsoleState(null, null, null));
            return;
        }

        GeneralStationRecord? record = null;
        if (console.ActiveKey != null)
        {
            _stationRecordsSystem.TryGetRecord(owningStation.Value, console.ActiveKey.Value, out record,
                stationRecordsComponent);
        }

        var logs = await _adminLogger.CurrentRoundLogs(new LogFilter {Types = new HashSet<LogType> { LogType.CriminalRecords }});

        _userInterface
            .GetUiOrNull(uid, GeneralStationRecordConsoleKey.Key)?
            .SetState(new GeneralStationRecordConsoleState(console.ActiveKey, record, listing));
    }
}
