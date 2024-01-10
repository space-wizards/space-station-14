using System.Linq;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Components;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;

namespace Content.Server.StationRecords.Systems;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RecordsConsoleSystem _recordsConsole = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordRemovedEvent>(UpdateUserInterface);

        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, SelectStationRecord>(OnKeySelected);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, SetStationRecordFilter>(OnFiltersChanged);
    }

    private void UpdateUserInterface<T>(Entity<GeneralStationRecordConsoleComponent> ent, ref T args)
    {
        UpdateUserInterface(ent);
    }

    // TODO: instead of copy paste shitcode for each record console, have a shared records console comp they all use
    // then have this somehow play nicely with creating ui state
    // if that gets done put it in RecordsConsoleSystem :)
    private void OnKeySelected(Entity<GeneralStationRecordConsoleComponent> ent, ref SelectStationRecord msg)
    {
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
    }

    private void OnFiltersChanged(Entity<GeneralStationRecordConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent);
        }
    }

    private void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            _ui.TrySetUiState(uid, GeneralStationRecordConsoleKey.Key, new GeneralStationRecordConsoleState());
            return;
        }

        var consoleRecords =
            _stationRecords.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecords);

        var listing = new Dictionary<uint, string>();

        foreach (var pair in consoleRecords)
        {
            if (_recordsConsole.IsSkipped(console.Filter, pair.Item2))
                continue;

            listing.Add(pair.Item1, pair.Item2.Name);
        }

        switch (listing.Count)
        {
            case 0:
                _ui.TrySetUiState(uid, GeneralStationRecordConsoleKey.Key, new GeneralStationRecordConsoleState());
                return;
            case 1:
                console.ActiveKey = listing.Keys.First();
                break;
        }

        if (console.ActiveKey is not { } id)
            return;

        var key = new StationRecordKey(id, owningStation.Value);
        _stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record, stationRecords);

        GeneralStationRecordConsoleState newState = new(id, record, listing, console.Filter);
        _ui.TrySetUiState(uid, GeneralStationRecordConsoleKey.Key, newState);
    }
}
