using Content.Shared.StationRecords;

namespace Content.Server.StationRecords.Systems;

public sealed class StationRecordKeyStorageSystem : EntitySystem
{
    /// <summary>
    ///     Assigns a station record key to an entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="key"></param>
    /// <param name="keyStorage"></param>
    public void AssignKey(EntityUid uid, StationRecordKey key, StationRecordKeyStorageComponent? keyStorage = null)
    {
        if (!Resolve(uid, ref keyStorage))
        {
            return;
        }

        keyStorage.Key = key;
    }

    /// <summary>
    ///     Removes a station record key from an entity.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="keyStorage"></param>
    /// <returns></returns>
    public StationRecordKey? RemoveKey(EntityUid uid, StationRecordKeyStorageComponent? keyStorage = null)
    {
        if (!Resolve(uid, ref keyStorage) || keyStorage.Key == null)
        {
            return null;
        }

        var key = keyStorage.Key;
        keyStorage.Key = null;

        return key;
    }

    /// <summary>
    ///     Checks if an entity currently contains a station record key.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="keyStorage"></param>
    /// <returns></returns>
    public bool CheckKey(EntityUid uid, StationRecordKeyStorageComponent? keyStorage = null)
    {
        if (!Resolve(uid, ref keyStorage))
        {
            return false;
        }

        return keyStorage.Key != null;
    }
}
