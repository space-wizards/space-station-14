using Content.Shared.Item;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage
{
    /// <summary>
    /// Handles generic storage with window, such as backpacks.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StorageComponent : Component
    {
        public static string ContainerId = "storagebase";

        // TODO: This fucking sucks
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public bool IsUiOpen;

        [ViewVariables]
        public Container Container = default!;

        /// <summary>
        /// A dictionary storing each entity to its position within the storage grid.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<EntityUid, ItemStorageLocation> StoredItems = new();

        /// <summary>
        /// A list of boxes that comprise a combined grid that determines the location that items can be stored.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public List<Box2i> Grid = new();

        /// <summary>
        /// The maximum size item that can be inserted into this storage,
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(SharedStorageSystem))]
        public ProtoId<ItemSizePrototype>? MaxItemSize;

        // TODO: Make area insert its own component.
        [DataField]
        public bool QuickInsert; // Can insert storables by "attacking" them with the storage entity

        [DataField]
        public bool ClickInsert = true; // Can insert stuff by clicking the storage entity with it

        [DataField]
        public bool AreaInsert;  // "Attacking" with the storage entity causes it to insert all nearby storables after a delay

        [DataField]
        public int AreaInsertRadius = 1;

        /// <summary>
        /// Whitelist for entities that can go into the storage.
        /// </summary>
        [DataField]
        public EntityWhitelist? Whitelist;

        /// <summary>
        /// Blacklist for entities that can go into storage.
        /// </summary>
        [DataField]
        public EntityWhitelist? Blacklist;

        /// <summary>
        /// Sound played whenever an entity is inserted into storage.
        /// </summary>
        [DataField]
        public SoundSpecifier? StorageInsertSound = new SoundCollectionSpecifier("storageRustle");

        /// <summary>
        /// Sound played whenever an entity is removed from storage.
        /// </summary>
        [DataField]
        public SoundSpecifier? StorageRemoveSound;

        /// <summary>
        /// Sound played whenever the storage window is opened.
        /// </summary>
        [DataField]
        public SoundSpecifier? StorageOpenSound = new SoundCollectionSpecifier("storageRustle");

        /// <summary>
        /// Sound played whenever the storage window is closed.
        /// </summary>
        [DataField]
        public SoundSpecifier? StorageCloseSound;

        /// <summary>
        /// If not null, ensures that all inserted items are of the same orientation
        /// Horizontal - items are stored laying down
        /// Vertical - items are stored standing up
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public StorageDefaultOrientation? DefaultStorageOrientation;

        [Serializable, NetSerializable]
        public enum StorageUiKey : byte
        {
            Key,
        }
    }

    [Serializable, NetSerializable]
    public sealed class StorageInteractWithItemEvent : EntityEventArgs
    {
        public readonly NetEntity InteractedItemUid;

        public readonly NetEntity StorageUid;

        public StorageInteractWithItemEvent(NetEntity interactedItemUid, NetEntity storageUid)
        {
            InteractedItemUid = interactedItemUid;
            StorageUid = storageUid;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StorageSetItemLocationEvent : EntityEventArgs
    {
        public readonly NetEntity ItemEnt;

        public readonly NetEntity StorageEnt;

        public readonly ItemStorageLocation Location;

        public StorageSetItemLocationEvent(NetEntity itemEnt, NetEntity storageEnt, ItemStorageLocation location)
        {
            ItemEnt = itemEnt;
            StorageEnt = storageEnt;
            Location = location;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StorageRemoveItemEvent : EntityEventArgs
    {
        public readonly NetEntity ItemEnt;

        public readonly NetEntity StorageEnt;

        public StorageRemoveItemEvent(NetEntity itemEnt, NetEntity storageEnt)
        {
            ItemEnt = itemEnt;
            StorageEnt = storageEnt;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StorageInsertItemIntoLocationEvent : EntityEventArgs
    {
        public readonly NetEntity ItemEnt;

        public readonly NetEntity StorageEnt;

        public readonly ItemStorageLocation Location;

        public StorageInsertItemIntoLocationEvent(NetEntity itemEnt, NetEntity storageEnt, ItemStorageLocation location)
        {
            ItemEnt = itemEnt;
            StorageEnt = storageEnt;
            Location = location;
        }
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

    /// <summary>
    /// An extra BUI message that either opens, closes, or focuses the storage window based on context.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class StorageModifyWindowMessage : BoundUserInterfaceMessage
    {

    }

    [NetSerializable]
    [Serializable]
    public enum StorageVisuals : byte
    {
        Open,
        HasContents,
        StorageUsed,
        Capacity
    }

    [Serializable, NetSerializable]
    public enum StorageDefaultOrientation : byte
    {
        Horizontal,
        Vertical
    }
}
