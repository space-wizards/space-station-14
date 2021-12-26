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

        public override string Name => "Storage";

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
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User))
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
    public class StorageComponentState : ComponentState
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
#pragma warning disable 618
    public class StorageHeldItemsMessage : ComponentMessage
#pragma warning restore 618
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
#pragma warning disable 618
    public class InsertEntityMessage : ComponentMessage
#pragma warning restore 618
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
#pragma warning disable 618
    public class AnimateInsertingEntitiesMessage : ComponentMessage
#pragma warning restore 618
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
#pragma warning disable 618
    public class RemoveEntityMessage : ComponentMessage
#pragma warning restore 618
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
#pragma warning disable 618
    public class OpenStorageUIMessage : ComponentMessage
#pragma warning restore 618
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
#pragma warning disable 618
    public class CloseStorageUIMessage : ComponentMessage
#pragma warning restore 618
    {
        public CloseStorageUIMessage()
        {
            Directed = true;
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
