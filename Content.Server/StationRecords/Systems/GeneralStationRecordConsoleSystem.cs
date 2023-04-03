using Content.Server.Station.Systems;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using System.Linq;

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
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, GeneralStationRecordsFilterMsg>(OnFiltersChanged);
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

    private void OnFiltersChanged(EntityUid uid,
        GeneralStationRecordConsoleComponent component, GeneralStationRecordsFilterMsg msg)
    {
        if (component.Filter == null ||
            component.Filter.type != msg.type || component.Filter.value != msg.value)
        {
            component.Filter = new GeneralStationRecordsFilter(msg.type, msg.value);
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
            if (console != null && console.Filter != null
                && IsSkippedRecord(console.Filter, pair.Item2))
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
        bool isSkipRecord = false;
        bool isFilter = filter.value.Length > 0;
        string filterLowerCaseValue = "";

        if (isFilter) {
            filterLowerCaseValue = filter.value.ToLower();
        }
        
        switch (filter.type)
        {
            case GeneralStationRecordFilterType.Name:
                if (someRecord.Name != null)
                {
                    string loweName = someRecord.Name.ToLower();
                    isSkipRecord = isFilter && !loweName.Contains(filterLowerCaseValue);
                }
                break;
            case GeneralStationRecordFilterType.Prints:
                if (someRecord.Fingerprint != null)
                {
                    isSkipRecord = isFilter
                        && IsFilterWithCodeValue(someRecord.Fingerprint, filterLowerCaseValue);
                }
                break;
            case GeneralStationRecordFilterType.DNA:
                if (someRecord.DNA != null)
                {
                    isSkipRecord = isFilter
                        && IsFilterWithCodeValue(someRecord.DNA,filterLowerCaseValue);
                }
                break;
        }

        return isSkipRecord;
    }

    private bool IsFilterWithCodeValue(string value, string filter)
    {
        string lowerValue = value.ToLower();
        return !lowerValue.StartsWith(filter);
    }
}
