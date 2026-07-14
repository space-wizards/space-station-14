using System.Linq;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleSystem : SharedGeneralStationRecordConsoleSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private static readonly GeneralStationRecordConsoleState EmptyState = new();

    protected override void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent)
    {
        var owningStation = StationSys.GetOwningStation(ent.Owner);

        if (!_ui.TryGetOpenUi(ent.Owner, GeneralStationRecordConsoleKey.Key, out var bui)
            || bui is not GeneralStationRecordConsoleBoundUserInterface recordBui)
            return;

        if (!RecordsQuery.TryComp(owningStation, out var stationRecords))
        {
            recordBui.SetState(EmptyState);
            return;
        }

        var listing = StationRecordsSys.BuildListing((owningStation.Value, stationRecords), ent.Comp.Filter);

        switch (listing.Count)
        {
            case 0:
                recordBui.SetState(EmptyState);
                return;
            default:
                ent.Comp.ActiveKey ??= listing.Keys.First();
                break;
        }

        if (ent.Comp.ActiveKey is not { } id)
            return;

        var key = new StationRecordKey(id, owningStation.Value);
        StationRecordsSys.TryGetRecord<GeneralStationRecord>(key, out var record, stationRecords);

        var newState = new GeneralStationRecordConsoleState(id, record, listing, ent.Comp.Filter, ent.Comp.CanDeleteEntries);
        recordBui.SetState(newState);
    }
}
