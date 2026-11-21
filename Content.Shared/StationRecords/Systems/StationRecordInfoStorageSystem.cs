using Robust.Shared.GameStates;

namespace Content.Shared.StationRecords.Systems;

public sealed partial class StationRecordInfoStorageSystem : EntitySystem
{
    [Dependency] private StationRecordsSystem _records = default!;

    [Dependency] private EntityQuery<StationRecordInfoStorageComponent> _storageQuery = default!;

    [SubscribeLocalEvent]
    private void OnGetState(Entity<StationRecordInfoStorageComponent> ent, ref ComponentGetState args)
    {
        args.State = new StationRecordInfoStorageComponentState(_records.Convert(ent.Comp.Key), ent.Comp.Record);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<StationRecordInfoStorageComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not StationRecordInfoStorageComponentState state)
            return;

        ent.Comp.Key = _records.Convert(state.Key);
        ent.Comp.Record = state.Record;
    }

    /// <summary>
    ///     Assigns a station record key to an entity.
    /// </summary>
    public void AssignKey(Entity<StationRecordInfoStorageComponent?> ent, StationRecordKey key)
    {
        if (!_storageQuery.Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Key = key;
        Dirty(ent);
    }

    /// <summary>
    ///     Assigns a station record key to an entity.
    /// </summary>
    public void AssignRecord(Entity<StationRecordInfoStorageComponent?> ent, GeneralStationRecord record)
    {
        if (!_storageQuery.Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Record = record;
        Dirty(ent);
    }

    /// <summary>
    ///     Removes a station record key from an entity.
    /// </summary>
    public StationRecordKey? RemoveKey(Entity<StationRecordInfoStorageComponent?> ent)
    {
        if (!_storageQuery.Resolve(ent, ref ent.Comp) || ent.Comp.Key == null)
            return null;

        var key = ent.Comp.Key;
        ent.Comp.Key = null;
        Dirty(ent);

        return key;
    }

    /// <summary>
    ///     Checks if an entity currently contains a station record key.
    /// </summary>
    public bool CheckKey(Entity<StationRecordInfoStorageComponent?> ent)
    {
        if (!_storageQuery.Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.Key != null;
    }
}
