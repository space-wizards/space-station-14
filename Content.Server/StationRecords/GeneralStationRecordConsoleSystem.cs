using System.Linq;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

namespace Content.Server.StationRecords;

public sealed class GeneralStationRecordConsoleSystem : SharedGeneralStationRecordConsoleSystem
{
    protected override void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent)
    {
        var owningStation = StationSys.GetOwningStation(ent.Owner);

        if (!RecordsQuery.TryComp(owningStation, out var stationRecords))
            return;

        var listing = StationRecordsSys.BuildListing((owningStation.Value, stationRecords), ent.Comp.Filter);

        switch (listing.Count)
        {
            case 0:
                return;
            default:
                // Yeah, the override exists just for this.
                ent.Comp.ActiveKey ??= listing.Keys.First();
                DirtyField(ent.AsNullable(), nameof(GeneralStationRecordConsoleComponent.ActiveKey));
                break;
        }
    }
}
