using Robust.Shared.GameStates;

namespace Content.Shared.StationRecords;

public sealed class StationRecordInfoStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedStationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationRecordInfoStorageComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<StationRecordInfoStorageComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, StationRecordInfoStorageComponent component, ref ComponentGetState args)
    {
        args.State = new StationRecordInfoStorageComponentState(_records.Convert(component.Key), component.Record);
    }

    private void OnHandleState(EntityUid uid, StationRecordInfoStorageComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StationRecordInfoStorageComponentState state)
            return;
        component.Key = _records.Convert(state.Key);
        component.Record = state.Record;
    }

    /// <summary>
    ///     Assigns a station record key to an entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="key"></param>
    /// <param name="keyStorage"></param>
    public void AssignKey(EntityUid uid, StationRecordKey key, StationRecordInfoStorageComponent? keyStorage = null)
    {
        if (!Resolve(uid, ref keyStorage))
        {
            return;
        }

        keyStorage.Key = key;
        Dirty(uid, keyStorage);
    }

    /// <summary>
    ///     Assigns a station record key to an entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="key"></param>
    /// <param name="keyStorage"></param>
    public void AssignRecord(EntityUid uid, GeneralStationRecord record, StationRecordInfoStorageComponent? keyStorage = null)
    {
        if (!Resolve(uid, ref keyStorage))
        {
            return;
        }

        keyStorage.Record = record;
        Dirty(uid, keyStorage);
    }

    /// <summary>
    ///     Removes a station record key from an entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="keyStorage"></param>
    /// <returns></returns>
    public StationRecordKey? RemoveKey(EntityUid uid, StationRecordInfoStorageComponent? keyStorage = null)
    {
        if (!Resolve(uid, ref keyStorage) || keyStorage.Key == null)
        {
            return null;
        }

        var key = keyStorage.Key;
        keyStorage.Key = null;
        Dirty(uid, keyStorage);

        return key;
    }

    /// <summary>
    ///     Checks if an entity currently contains a station record key.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="keyStorage"></param>
    /// <returns></returns>
    public bool CheckKey(EntityUid uid, StationRecordInfoStorageComponent? keyStorage = null)
    {
        if (!Resolve(uid, ref keyStorage))
        {
            return false;
        }

        return keyStorage.Key != null;
    }
}
