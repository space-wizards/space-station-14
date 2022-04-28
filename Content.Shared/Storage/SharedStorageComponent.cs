using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Placeable;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage
{
    [NetworkedComponent()]
    public abstract class SharedStorageComponent : Component, IDraggable
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
        public sealed class StorageRemoveItemMessage : BoundUserInterfaceMessage
        {
            public readonly EntityUid InteractedItemUID;
            public StorageRemoveItemMessage(EntityUid interactedItemUID)
            {
                InteractedItemUID = interactedItemUID;
            }
        }

        [Serializable, NetSerializable]
        public enum StorageUiKey
        {
            Key,
        }

        [Dependency] private readonly IEntityManager _entMan = default!;
        public abstract IReadOnlyList<EntityUid>? StoredEntities { get; }

        /// <summary>
        ///     Removes from the storage container and updates the stored value
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        /// <returns>True if no longer in storage, false otherwise</returns>
        public abstract bool Remove(EntityUid entity);

        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return _entMan.TryGetComponent(args.Target, out PlaceableSurfaceComponent? placeable) &&
                   placeable.IsPlaceable;
        }

        bool IDraggable.Drop(DragDropEvent eventArgs)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User, eventArgs.Target))
                return false;

            var storedEntities = StoredEntities?.ToArray();

            if (storedEntities == null)
                return false;

            // empty everything out
            foreach (var storedEntity in storedEntities)
            {
                if (Remove(storedEntity))
                    _entMan.GetComponent<TransformComponent>(storedEntity).WorldPosition = eventArgs.DropLocation.Position;
            }

            return true;
        }
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
    public enum StorageVisuals
    {
        Open,
        CanWeld,
        Welded,
        CanLock,
        Locked
    }
}
