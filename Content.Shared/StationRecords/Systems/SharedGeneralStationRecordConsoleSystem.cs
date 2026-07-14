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
        => UpdateUserInterface(ent);

    [SubscribeLocalEvent]
    private void OnGeneralRecordCreated(Entity<GeneralStationRecordConsoleComponent> ent, ref GeneralRecordCreatedEvent args)
        => UpdateUserInterface(ent);

    [SubscribeLocalEvent]
    private void OnRecordRemoved(Entity<GeneralStationRecordConsoleComponent> ent, ref RecordRemovedEvent args)
        => UpdateUserInterface(ent);

    [SubscribeLocalEvent]
    private void OnRecordDelete(Entity<GeneralStationRecordConsoleComponent> ent, ref DeleteStationRecord args)
    {
        if (!ent.Comp.CanDeleteEntries)
            return;

        var owning = StationSys.GetOwningStation(ent.Owner);
        if (owning != null)
            StationRecordsSys.RemoveRecord(new StationRecordKey(args.Id, owning.Value));
    }

    // TODO: instead of copy paste shitcode for each record console, have a shared records console comp they all use
    // then have this somehow play nicely with creating ui state
    // if that gets done put it in StationRecordsSystem console helpers section :)
    [SubscribeLocalEvent]
    private void OnKeySelected(Entity<GeneralStationRecordConsoleComponent> ent, ref SelectStationRecord msg)
    {
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
        DirtyField(ent.AsNullable(), nameof(GeneralStationRecordConsoleComponent.ActiveKey));
    }

    [SubscribeLocalEvent]
    private void OnFiltersChanged(Entity<GeneralStationRecordConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter != null
            && ent.Comp.Filter.Type == msg.Type
            && ent.Comp.Filter.Value == msg.Value)
            return;

        ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
        UpdateUserInterface(ent);
        DirtyField(ent.AsNullable(), nameof(GeneralStationRecordConsoleComponent.Filter));
    }

    protected virtual void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent) { }
}
