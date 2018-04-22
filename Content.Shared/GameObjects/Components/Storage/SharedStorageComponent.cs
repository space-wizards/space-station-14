using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.Components.Storage
{
    public abstract class SharedStorageComponent : Component
    {
        public sealed override string Name => "Storage";
        public override uint? NetID => ContentNetIDs.STORAGE;
    }

    /// <summary>
    /// Updates the client component about what entities this storage is holding
    /// </summary>
    [Serializable, NetSerializable]
    public class StorageHeldItemsMessage : ComponentMessage
    {
        public readonly int StorageSizeMax;
        public readonly int StorageSizeUsed;
        public Dictionary<EntityUid, int> StoredEntities;

        public StorageHeldItemsMessage(Dictionary<EntityUid, int> storedentities, int storageused, int storagemaxsize)
        {
            Directed = true;
            StorageSizeMax = storagemaxsize;
            StorageSizeUsed = storageused;
            StoredEntities = storedentities;
        }
    }

    /// <summary>
    /// Component message for removing a contained entity from the storage entity
    /// </summary>
    [Serializable, NetSerializable]
    public class RemoveEntityMessage : ComponentMessage
    {
        public EntityUid EntityUid;

        public RemoveEntityMessage(EntityUid entityuid)
        {
            Directed = true;
            EntityUid = entityuid;
        }
    }

    /// <summary>
    /// Component message for opening the storage UI
    /// </summary>
    [Serializable, NetSerializable]
    public class OpenStorageUIMessage : ComponentMessage
    {
        public OpenStorageUIMessage()
        {
            Directed = true;
        }
    }
}
