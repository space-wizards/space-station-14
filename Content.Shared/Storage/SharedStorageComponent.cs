using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Interaction.Events;
using Content.Shared.Placeable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage
{
    [NetworkedComponent()]
    public abstract class SharedStorageComponent : Component, IDraggable
    {
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
            {
                return false;
            }

            var storedEntities = StoredEntities?.ToArray();

            if (storedEntities == null)
            {
                return false;
            }

            // empty everything out
            foreach (var storedEntity in storedEntities)
            {
                if (Remove(storedEntity))
                {
                    _entMan.GetComponent<TransformComponent>(storedEntity).WorldPosition = eventArgs.DropLocation.Position;
                }
            }

            return true;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StorageComponentState : ComponentState
    {
        public readonly EntityUid[] StoredEntities;

        public StorageComponentState(EntityUid[] storedEntities)
        {
            StoredEntities = storedEntities;
        }
    }

    /// <summary>
    /// Updates the client component about what entities this storage is holding
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class StorageHeldItemsEvent : EntityEventArgs
    {
        public readonly EntityUid Storage;
        public readonly int StorageSizeMax;
        public readonly int StorageSizeUsed;
        public readonly EntityUid[] StoredEntities;

        public StorageHeldItemsEvent(EntityUid storage, int storageSizeMax, int storageSizeUsed, EntityUid[] storedEntities)
        {
            Storage = storage;
            StorageSizeMax = storageSizeMax;
            StorageSizeUsed = storageSizeUsed;
            StoredEntities = storedEntities;
        }
    }

    /// <summary>
    /// Network event for adding an entity to the storage entity.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class InsertEntityEvent : EntityEventArgs
    {
        public readonly EntityUid Storage;

        public InsertEntityEvent(EntityUid storage)
        {
            Storage = storage;
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

    /// <summary>
    /// Network event for removing a contained entity from the storage entity
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RemoveEntityEvent : EntityEventArgs
    {
        public EntityUid Storage;
        public EntityUid EntityUid;

        public RemoveEntityEvent(EntityUid storage, EntityUid entityUid)
        {
            Storage = storage;
            EntityUid = entityUid;
        }
    }

    /// <summary>
    /// Network event for opening the storage UI
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class OpenStorageUIEvent : EntityEventArgs
    {
        public readonly EntityUid Storage;

        public OpenStorageUIEvent(EntityUid storage)
        {
            Storage = storage;
        }
    }

    /// <summary>
    /// Network event for closing the storage UI.
    /// E.g when the player moves too far away from the container.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CloseStorageUIEvent : EntityEventArgs
    {
        public readonly EntityUid Storage;

        public CloseStorageUIEvent(EntityUid storage)
        {
            Storage = storage;
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
