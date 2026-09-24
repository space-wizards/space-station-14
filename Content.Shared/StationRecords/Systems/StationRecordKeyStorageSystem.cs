using Content.Shared.StationRecords.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.StationRecords.Systems;

public sealed partial class StationRecordKeyStorageSystem : EntitySystem
{
    [Dependency] private StationRecordsSystem _records = default!;

    [Dependency] private EntityQuery<StationRecordKeyStorageComponent> _storageQuery = default!;

    [SubscribeLocalEvent]
    private void OnGetState(Entity<StationRecordKeyStorageComponent> ent, ref ComponentGetState args)
    {
        args.State = new StationRecordKeyStorageComponentState(_records.Convert(ent.Comp.Key));
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<StationRecordKeyStorageComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not StationRecordKeyStorageComponentState state)
            return;

        ent.Comp.Key = _records.Convert(state.Key);
    }

    /// <summary>
    ///     Assigns a station record key to an entity.
    /// </summary>
    public void AssignKey(Entity<StationRecordKeyStorageComponent?> ent, StationRecordKey key)
    {
        if (!_storageQuery.Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Key = key;
        Dirty(ent);
    }

    /// <summary>
    ///     Removes a station record key from an entity.
    /// </summary>
    public StationRecordKey? RemoveKey(Entity<StationRecordKeyStorageComponent?> ent)
    {
        if (!_storageQuery.Resolve(ent, ref ent.Comp)
            || ent.Comp.Key == null)
            return null;

        var key = ent.Comp.Key;
        ent.Comp.Key = null;
        Dirty(ent);

        return key;
    }

    /// <summary>
    ///     Checks if an entity currently contains a station record key.
    /// </summary>
    public bool CheckKey(Entity<StationRecordKeyStorageComponent?> ent)
    {
        if (!_storageQuery.Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.Key != null;
    }
}
