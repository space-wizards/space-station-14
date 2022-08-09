using Content.Server.Station.Systems;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;

namespace Content.Server.StationRecords.Systems;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, SelectGeneralStationRecord>(OnKeySelected);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
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

        if (enumerator == null)
        {
            _userInterface.GetUiOrNull(uid, GeneralStationRecordConsoleKey.Key)?.SetState(new GeneralStationRecordConsoleState(null, null, null));
            return;
        }

        var listing = new Dictionary<StationRecordKey, string>();
        foreach (var pair in enumerator)
        {
            listing.Add(pair.Value.Item1, pair.Value.Item2.Name);
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
