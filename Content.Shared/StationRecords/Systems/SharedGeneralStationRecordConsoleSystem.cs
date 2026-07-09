using Content.Shared.Station;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;

namespace Content.Shared.StationRecords.Systems;

public abstract partial class SharedGeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] protected SharedStationSystem StationSys = default!;
    [Dependency] protected StationRecordsSystem StationRecordsSys = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, GeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordRemovedEvent>(UpdateUserInterface);

        Subs.BuiEvents<GeneralStationRecordConsoleComponent>(GeneralStationRecordConsoleKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
            subs.Event<DeleteStationRecord>(OnRecordDelete);
        });
    }

    private void OnRecordDelete(Entity<GeneralStationRecordConsoleComponent> ent, ref DeleteStationRecord args)
    {
        if (!ent.Comp.CanDeleteEntries)
            return;

        var owning = StationSys.GetOwningStation(ent.Owner);
        if (owning != null)
            StationRecordsSys.RemoveRecord(new StationRecordKey(args.Id, owning.Value));

        UpdateUserInterface(ent); // Apparently an event does not get raised for this.
    }

    private void UpdateUserInterface<T>(Entity<GeneralStationRecordConsoleComponent> ent, ref T args)
    {
        UpdateUserInterface(ent);
    }

    // TODO: instead of copy paste shitcode for each record console, have a shared records console comp they all use
    // then have this somehow play nicely with creating ui state
    // if that gets done put it in StationRecordsSystem console helpers section :)
    private void OnKeySelected(Entity<GeneralStationRecordConsoleComponent> ent, ref SelectStationRecord msg)
    {
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
    }

    private void OnFiltersChanged(Entity<GeneralStationRecordConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter != null
            && ent.Comp.Filter.Type == msg.Type
            && ent.Comp.Filter.Value == msg.Value)
            return;

        ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
        UpdateUserInterface(ent);
    }

    protected virtual void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent) { }
}
