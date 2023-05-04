using Content.Server.CriminalRecords.Components;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.Security;
using Content.Server.Security.Components;
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

public sealed class GeneralCriminalRecordConsoleSystem : EntitySystem
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
        if (msg.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("general-criminal-record-permission-denied"), uid, msg.Session);
            return;
        }

        TryComp<SecurityInfoComponent>(msg.Session.AttachedEntity, out var secInfo);

        if (msg.Reason != string.Empty && msg.Name != null && msg.Session.AttachedEntity != null && secInfo != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.Detained ? SecurityStatus.None : SecurityStatus.Detained;
            secInfo.Reason = msg.Reason!;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Detained, Loc.GetString("general-criminal-record-console-detained-with-reason", ("name", msg.Name), ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) },
                { SecurityStatus.None, Loc.GetString("general-criminal-record-console-undetained-with-reason", ("name", msg.Name), ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status], _prototypeManager.Index<RadioChannelPrototype>("Security"));
        }

        else if (msg.Reason == string.Empty && msg.Name != null && msg.Session.AttachedEntity != null && secInfo != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.Detained ? SecurityStatus.None : SecurityStatus.Detained;
            secInfo.Reason = msg.Reason;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Detained, Loc.GetString("general-criminal-record-console-detained-without-reason", ("name", msg.Name), ("goodguyname", Name(msg.Session.AttachedEntity.Value))) },
                { SecurityStatus.None, Loc.GetString("general-criminal-record-console-undetained-without-reason", ("name", msg.Name), ("goodguyname", Name(msg.Session.AttachedEntity.Value))) }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status], _prototypeManager.Index<RadioChannelPrototype>("Security"));
        }

        var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

        TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

        _stationRecordsSystem.TryGetRecord(station!.Value, component.ActiveKey!.Value, out GeneralCriminalRecord? record, stationRecordsComponent);

        record!.Reason = secInfo!.Reason;
        record.Status = secInfo.Status;

        _stationRecordsSystem.Synchronize(station.Value);
        UpdateUserInterface(uid, component);
    }

    private void OnStatusSelected(EntityUid uid, GeneralCriminalRecordConsoleComponent component,
        CriminalStatusOptionButtonSelected msg)
    {
        if (msg.Session.AttachedEntity is not {Valid: true} mob) return;
        if (!CanUse(mob, uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("general-criminal-record-permission-denied"), uid, msg.Session);
            return;
        }

        TryComp<SecurityInfoComponent>(msg.Session.AttachedEntity, out var secInfo);

        if (msg.Status == secInfo!.Status)
            return;

        if (msg.Reason != string.Empty && msg.Session.AttachedEntity != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.None ? SecurityStatus.Wanted : SecurityStatus.None;
            secInfo.Reason = msg.Reason!;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Wanted, Loc.GetString("general-criminal-record-console-wanted-with-reason", ("name", msg.Name)!, ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) },
                { SecurityStatus.None, Loc.GetString("general-criminal-record-console-not-wanted-with-reason", ("name", msg.Name)!, ("reason", msg.Reason)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status],
                _prototypeManager.Index<RadioChannelPrototype>("Security"));

            var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

            TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

            _stationRecordsSystem.TryGetRecord(station!.Value, component.ActiveKey!.Value, out GeneralCriminalRecord? record, stationRecordsComponent);

            record!.Reason = secInfo.Status == SecurityStatus.None ? string.Empty : secInfo.Reason;
            record.Status = secInfo.Status;

            _stationRecordsSystem.Synchronize(station.Value);
            UpdateUserInterface(uid, component);
        }

        else if (msg.Reason == string.Empty && msg.Session.AttachedEntity != null)
        {
            secInfo.Status = secInfo.Status == SecurityStatus.None ? SecurityStatus.Wanted : SecurityStatus.None;
            secInfo.Reason = msg.Reason;

            var messages = new Dictionary<SecurityStatus, string>()
            {
                { SecurityStatus.Wanted, Loc.GetString("general-criminal-record-console-wanted-without-reason", ("name", msg.Name)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) },
                { SecurityStatus.None, Loc.GetString("general-criminal-record-console-not-wanted-without-reason", ("name", msg.Name)!, ("goodguyname", Name(msg.Session.AttachedEntity.Value))) }
            };

            _radioSystem.SendRadioMessage(uid, messages[secInfo.Status],
                _prototypeManager.Index<RadioChannelPrototype>("Security"));
            var station = _stationSystem.GetOwningStation(msg.Session.AttachedEntity!.Value);

            TryComp<StationRecordsComponent>(station, out var stationRecordsComponent);

            _stationRecordsSystem.TryGetRecord(station!.Value, component.ActiveKey!.Value, out GeneralCriminalRecord? record, stationRecordsComponent);

            record!.Reason = secInfo.Reason;
            record.Status = secInfo.Status;

            _stationRecordsSystem.Synchronize(station.Value);
            UpdateUserInterface(uid, component);
        }
    }

    private bool CanUse(EntityUid user, EntityUid console)
    {
        if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent) && !HasComp<EmaggedComponent>(console))
        {
            return _accessReaderSystem.IsAllowed(user, accessReaderComponent);
        }
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
