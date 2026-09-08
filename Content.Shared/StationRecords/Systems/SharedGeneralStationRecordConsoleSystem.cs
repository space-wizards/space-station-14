using Content.Shared.Station;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;

namespace Content.Shared.StationRecords.Systems;

public abstract partial class SharedGeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] protected SharedStationSystem StationSys = default!;
    [Dependency] protected StationRecordsSystem StationRecordsSys = default!;

    [Dependency] protected EntityQuery<StationRecordsComponent> RecordsQuery = default!;

    [SubscribeLocalEvent]
    private void OnRecordModified(Entity<GeneralStationRecordConsoleComponent> ent, ref RecordModifiedEvent args)
    {
        UpdateUserInterface(ent);
    }

    [SubscribeLocalEvent]
    private void OnGeneralRecordCreated(Entity<GeneralStationRecordConsoleComponent> ent, ref GeneralRecordCreatedEvent args)
    {
        UpdateUserInterface(ent);
    }

    [SubscribeLocalEvent]
    private void OnRecordRemoved(Entity<GeneralStationRecordConsoleComponent> ent, ref RecordRemovedEvent args)
    {
        UpdateUserInterface(ent);
    }

    [SubscribeLocalEvent]
    private void OnRecordDelete(Entity<GeneralStationRecordConsoleComponent> ent, ref DeleteStationRecord args)
    {
        if (!ent.Comp.CanDeleteEntries)
            return;

        var owning = StationSys.GetOwningStation(ent.Owner);
        if (owning != null)
            StationRecordsSys.RemoveRecord(new StationRecordKey(args.Id, owning.Value));

        UpdateUserInterface(ent);
    }

    protected virtual void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent) { }
}
