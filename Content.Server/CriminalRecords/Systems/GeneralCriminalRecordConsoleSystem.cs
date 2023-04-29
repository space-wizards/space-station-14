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
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.CriminalRecords;
using Content.Shared.Database;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.CriminalRecords.Systems;

public sealed class GeneralCriminalRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, SelectGeneralCriminalRecord>(OnKeySelected);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, CriminalRecordArrestButtonPressed>(OnButtonPressed);
        SubscribeLocalEvent<GeneralCriminalRecordConsoleComponent, CriminalStatusOptionButtonSelected>(OnStatusSelected);
    }

    private void UpdateUserInterface<T>(EntityUid uid, GeneralCriminalRecordConsoleComponent component, T ev)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnKeySelected(EntityUid uid, GeneralCriminalRecordConsoleComponent component,
        SelectGeneralCriminalRecord msg)
    {
        component.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(uid, component);
    }

    private void OnButtonPressed(EntityUid uid, GeneralCriminalRecordConsoleComponent component,
        CriminalRecordArrestButtonPressed msg)
    {
        TryComp<SecurityInfoComponent>(msg.Session.AttachedEntity, out var secInfo);

        if (msg.Reason != string.Empty && msg.Name != null && msg.Session.AttachedEntity != null && secInfo != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.Detained ? SecurityStatus.None : SecurityStatus.Detained;
            secInfo.Reason = msg.Reason!;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Detained, $"{msg.Name} has been detained for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}" },
                { SecurityStatus.None, $"{msg.Name} has been released from the detention for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}" }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status], _prototypeManager.Index<RadioChannelPrototype>("Security"));
        }

        else if (msg.Reason == string.Empty && msg.Name != null && msg.Session.AttachedEntity != null && secInfo != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.Detained ? SecurityStatus.None : SecurityStatus.Detained;
            secInfo.Reason = msg.Reason;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Detained, $"{msg.Name} has been detained for by {Name(msg.Session.AttachedEntity.Value)}" },
                { SecurityStatus.None, $"{msg.Name} has been released from the detention by {Name(msg.Session.AttachedEntity.Value)}" }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status], _prototypeManager.Index<RadioChannelPrototype>("Security"));
        }

        var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

        TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

        GeneralCriminalRecord? record;
        _stationRecordsSystem.TryGetRecord(station!.Value, component.ActiveKey!.Value, out record, stationRecordsComponent);

        record!.Reason = secInfo!.Reason;
        record.Status = secInfo.Status;

        _stationRecordsSystem.Synchronize(station!.Value);
        UpdateUserInterface(uid, component);
    }

    private void OnStatusSelected(EntityUid uid, GeneralCriminalRecordConsoleComponent component,
        CriminalStatusOptionButtonSelected msg)
    {
        TryComp<SecurityInfoComponent>(msg.Session.AttachedEntity, out var secInfo);

        if (msg.Reason != string.Empty && secInfo != null && msg.Session.AttachedEntity != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.None ? SecurityStatus.Wanted : SecurityStatus.None;
            secInfo.Reason = msg.Reason!;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Wanted, $"{msg.Name} is wanted for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}" },
                { SecurityStatus.None, $"{msg.Name} is not wanted anymore for {msg.Reason} by {Name(msg.Session.AttachedEntity.Value)}" }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status],
                _prototypeManager.Index<RadioChannelPrototype>("Security"));

            var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

            TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

            GeneralCriminalRecord? record;
            _stationRecordsSystem.TryGetRecord(station!.Value, component.ActiveKey!.Value, out record, stationRecordsComponent);

            if (secInfo.Status == SecurityStatus.None)
                record!.Reason = string.Empty;
            else
                record!.Reason = secInfo.Reason;
            record.Status = secInfo.Status;

            _stationRecordsSystem.Synchronize(station!.Value);
            UpdateUserInterface(uid, component);
        }

        else if (msg.Reason == string.Empty && secInfo != null && msg.Session.AttachedEntity != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.None ? SecurityStatus.Wanted : SecurityStatus.None;
            secInfo.Reason = msg.Reason;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Wanted, $"{msg.Name} is wanted by {Name(msg.Session.AttachedEntity.Value)}" },
                { SecurityStatus.None, $"{msg.Name} is not wanted anymore by {Name(msg.Session.AttachedEntity.Value)}" }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status],
                _prototypeManager.Index<RadioChannelPrototype>("Security"));
            var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

            TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

            GeneralCriminalRecord? record;
            _stationRecordsSystem.TryGetRecord(station!.Value, component.ActiveKey!.Value, out record, stationRecordsComponent);

            record!.Reason = secInfo!.Reason;
            record.Status = secInfo.Status;

            _stationRecordsSystem.Synchronize(station!.Value);
            UpdateUserInterface(uid, component);
        }
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
            _userInterface.GetUiOrNull(uid, GeneralCriminalRecordConsoleKey.Key)?.SetState(new GeneralCriminalRecordConsoleState(null, null, null, null));
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
            _userInterface.GetUiOrNull(uid, GeneralCriminalRecordConsoleKey.Key)?.SetState(new GeneralCriminalRecordConsoleState(null, null, null, null));
            return;
        }

        GeneralStationRecord? stationRecord = null;
        if (console.ActiveKey != null)
        {
            _stationRecordsSystem.TryGetRecord(owningStation.Value, console.ActiveKey.Value, out stationRecord,
                stationRecordsComponent);
        }

        GeneralCriminalRecord? criminalRecord = null;
        if (console.ActiveKey != null)
        {
            _stationRecordsSystem.TryGetRecord(owningStation.Value, console.ActiveKey.Value, out criminalRecord,
                stationRecordsComponent);
        }

        _userInterface
            .GetUiOrNull(uid, GeneralCriminalRecordConsoleKey.Key)?
            .SetState(new GeneralCriminalRecordConsoleState(console.ActiveKey, stationRecord, criminalRecord, listing));
    }
}
