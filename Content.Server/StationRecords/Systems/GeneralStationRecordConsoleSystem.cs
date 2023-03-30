using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.StationRecords.Systems;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, SelectGeneralStationRecord>(OnKeySelected);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, StationRecordArrestButtonPressed>(OnButtonPressed);
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
        if (msg.Reason != null && msg.Name != null)
        {
            _radioSystem.SendRadioMessage(component.Owner, $"{msg.Name} has been detained for {msg.Reason}",
                _prototypeManager.Index<RadioChannelPrototype>("Security"));
        }
    }

    private void UpdateUserInterface(EntityUid uid, GeneralStationRecordConsoleComponent? console = null)
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

        _userInterface
            .GetUiOrNull(uid, GeneralStationRecordConsoleKey.Key)?
            .SetState(new GeneralStationRecordConsoleState(console.ActiveKey, record, listing));
    }
}
