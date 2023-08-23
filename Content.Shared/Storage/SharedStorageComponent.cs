using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage
{
    [NetworkedComponent()]
    public abstract partial class SharedStorageComponent : Component
    {
        [Serializable, NetSerializable]
        public sealed class StorageBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly List<NetEntity> StoredEntities;
            public readonly int StorageSizeUsed;
            public readonly int StorageCapacityMax;

            public StorageBoundUserInterfaceState(List<NetEntity> storedEntities, int storageSizeUsed, int storageCapacityMax)
            {
                StoredEntities = storedEntities;
                StorageSizeUsed = storageSizeUsed;
                StorageCapacityMax = storageCapacityMax;
            }
        }

        [Serializable, NetSerializable]
        public sealed class StorageInsertItemMessage : BoundUserInterfaceMessage
        {
        }

        [Serializable, NetSerializable]
        public sealed class StorageInteractWithItemEvent : BoundUserInterfaceMessage
        {
            public readonly NetEntity InteractedItemUID;
            public StorageInteractWithItemEvent(NetEntity interactedItemUID)
            {
                InteractedItemUID = interactedItemUID;
            }
        }

        [Serializable, NetSerializable]
        public enum StorageUiKey
        {
            Key,
        }

        public abstract IReadOnlyList<EntityUid>? StoredEntities { get; }

        /// <summary>
        ///     Removes from the storage container and updates the stored value
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        /// <returns>True if no longer in storage, false otherwise</returns>
        public abstract bool Remove(EntityUid entity);
    }

    /// <summary>
    /// Network event for displaying an animation of entities flying into a storage entity
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class AnimateInsertingEntitiesEvent : EntityEventArgs
    {
        public readonly NetEntity Storage;
        public readonly List<NetEntity> StoredEntities;
        public readonly List<NetCoordinates> EntityPositions;
        public readonly List<Angle> EntityAngles;

        public AnimateInsertingEntitiesEvent(NetEntity storage, List<NetEntity> storedEntities, List<NetCoordinates> entityPositions, List<Angle> entityAngles)
        {
            Storage = storage;
            StoredEntities = storedEntities;
            EntityPositions = entityPositions;
            EntityAngles = entityAngles;
        }
    }

    [NetSerializable]
    [Serializable]
    public enum StorageVisuals : byte
    {
        Open,
        HasContents,
        CanLock,
        Locked
    }
}
