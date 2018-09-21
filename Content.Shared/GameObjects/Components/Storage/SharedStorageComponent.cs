using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using SS14.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Storage
{
    public abstract class SharedStorageComponent : Component
    {
        public sealed override string Name => "Storage";
        public override uint? NetID => ContentNetIDs.INVENTORY;
        public override Type StateType => typeof(StorageComponentState);

        private bool _open;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Open
        {
            get => _open;
            set
            {
                _open = value;
                Dirty();
            }
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _open, "open", false);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new StorageComponentState(_open);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            base.HandleComponentState(state);

            if (!(state is StorageComponentState storageState))
                return;

            _open = storageState.Open;
        }
    }

    [Serializable, NetSerializable]
    public class StorageComponentState : ComponentState
    {
        public bool Open { get; }

        public StorageComponentState(bool open)
            : base(ContentNetIDs.INVENTORY)
        {
            Open = open;
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
}
