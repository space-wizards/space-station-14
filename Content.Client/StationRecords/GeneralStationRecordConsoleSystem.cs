using System.Linq;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleSystem : SharedGeneralStationRecordConsoleSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityQuery<StationRecordsComponent> _recordsQuery = default!;

    private static readonly GeneralStationRecordConsoleState EmptyState = new();

    protected override void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = StationSys.GetOwningStation(uid);

        if (!_ui.TryGetOpenUi(ent.Owner, GeneralStationRecordConsoleKey.Key, out var bui)
            || bui is not GeneralStationRecordConsoleBoundUserInterface recordBui)
            return;

        if (!_recordsQuery.TryComp(owningStation, out var stationRecords))
        {
            recordBui.SetState(EmptyState);
            return;
        }

        var listing = StationRecordsSys.BuildListing((owningStation.Value, stationRecords), console.Filter);

        switch (listing.Count)
        {
            case 0:
                recordBui.SetState(EmptyState);
                return;
            default:
                console.ActiveKey ??= listing.Keys.First();
                break;
        }

        if (console.ActiveKey is not { } id)
            return;

        var key = new StationRecordKey(id, owningStation.Value);
        StationRecordsSys.TryGetRecord<GeneralStationRecord>(key, out var record, stationRecords);

        var newState = new GeneralStationRecordConsoleState(id, record, listing, console.Filter, ent.Comp.CanDeleteEntries);
        recordBui.SetState(newState);
    }
}
