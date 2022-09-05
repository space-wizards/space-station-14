using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage
{
    [NetworkedComponent()]
    public abstract class SharedStorageComponent : Component
    {
        [Serializable, NetSerializable]
        public sealed class StorageBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly List<EntityUid> StoredEntities;
            public readonly int StorageSizeUsed;
            public readonly int StorageCapacityMax;

            public StorageBoundUserInterfaceState(List<EntityUid> storedEntities, int storageSizeUsed, int storageCapacityMax)
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
            public readonly EntityUid InteractedItemUID;
            public StorageInteractWithItemEvent(EntityUid interactedItemUID)
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
        public readonly EntityUid Storage;
        public readonly List<EntityUid> StoredEntities;
        public readonly List<EntityCoordinates> EntityPositions;

        public AnimateInsertingEntitiesEvent(EntityUid storage, List<EntityUid> storedEntities, List<EntityCoordinates> entityPositions)
        {
            Storage = storage;
            StoredEntities = storedEntities;
            EntityPositions = entityPositions;
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
