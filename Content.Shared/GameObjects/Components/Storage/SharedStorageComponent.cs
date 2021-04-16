#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Storage
{
    public abstract class SharedStorageComponent : Component, IDraggable
    {
        public override string Name => "Storage";
        public override uint? NetID => ContentNetIDs.INVENTORY;

        public abstract IReadOnlyList<IEntity>? StoredEntities { get; }

        /// <summary>
        ///     Removes from the storage container and updates the stored value
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        /// <returns>True if no longer in storage, false otherwise</returns>
        public abstract bool Remove(IEntity entity);

        bool IDraggable.CanDrop(CanDropEventArgs args)
        {
            return args.Target.TryGetComponent(out SharedPlaceableSurfaceComponent? placeable) &&
                   placeable.IsPlaceable;
        }

        bool IDraggable.Drop(DragDropEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
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
                    storedEntity.Transform.WorldPosition = eventArgs.DropLocation.Position;
                }
            }

            return true;
        }
    }

    [Serializable, NetSerializable]
    public class StorageComponentState : ComponentState
    {
        public readonly EntityUid[] StoredEntities;

        public StorageComponentState(EntityUid[] storedEntities) : base(ContentNetIDs.INVENTORY)
        {
            StoredEntities = storedEntities;
        }
    }

    /// <summary>
    /// Updates the client component about what entities this storage is holding
    /// </summary>
    [Serializable, NetSerializable]
    public class StorageHeldItemsMessage : ComponentMessage
    {
        public readonly int StorageSizeMax;
        public readonly int StorageSizeUsed;
        public readonly EntityUid[] StoredEntities;

        public StorageHeldItemsMessage(EntityUid[] storedEntities, int storageUsed, int storageMaxSize)
        {
            Directed = true;
            StorageSizeMax = storageMaxSize;
            StorageSizeUsed = storageUsed;
            StoredEntities = storedEntities;
        }
    }

    /// <summary>
    /// Component message for adding an entity to the storage entity.
    /// </summary>
    [Serializable, NetSerializable]
    public class InsertEntityMessage : ComponentMessage
    {
        public InsertEntityMessage()
        {
            Directed = true;
        }
    }

    /// <summary>
    /// Component message for displaying an animation of entities flying into a storage entity
    /// </summary>
    [Serializable, NetSerializable]
    public class AnimateInsertingEntitiesMessage : ComponentMessage
    {
        public readonly List<EntityUid> StoredEntities;
        public readonly List<EntityCoordinates> EntityPositions;
        public AnimateInsertingEntitiesMessage(List<EntityUid> storedEntities, List<EntityCoordinates> entityPositions)
        {
            Directed = true;
            StoredEntities = storedEntities;
            EntityPositions = entityPositions;
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

    /// <summary>
    /// Component message for closing the storage UI.
    /// E.g when the player moves too far away from the container.
    /// </summary>
    [Serializable, NetSerializable]
    public class CloseStorageUIMessage : ComponentMessage
    {
        public CloseStorageUIMessage()
        {
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public struct StorageFillLevel
    {
        public readonly int StorageSizeMax;
        public readonly int StorageSizeUsed;

        public StorageFillLevel(int storageUsed, int storageMaxSize)
        {
            StorageSizeMax = storageMaxSize;
            StorageSizeUsed = storageUsed;
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
        Locked,
        FillLevel
    }
}
