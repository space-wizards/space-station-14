using Content.Shared.Item;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
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

        public const byte ChunkSize = 8;

        // No datafield because we can just derive it from stored items.
        /// <summary>
        /// Bitmask of occupied tiles
        /// </summary>
        public Dictionary<Vector2i, ulong> OccupiedGrid = new();

        [ViewVariables]
        public Container Container = default!;

        /// <summary>
        /// A dictionary storing each entity to its position within the storage grid.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<EntityUid, ItemStorageLocation> StoredItems = new();

        /// <summary>
        /// A dictionary storing each saved item to its location in the grid.
        /// When trying to quick insert an item, if there is an empty location with the same name it will be placed there.
        /// Multiple items with the same name can be saved, they will be checked individually.
        /// </summary>
        [DataField]
        public Dictionary<string, List<ItemStorageLocation>> SavedLocations = new();

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
        public bool QuickInsert; // Can insert storables by clicking them with the storage entity

        /// <summary>
        /// Minimum delay between quick/area insert actions.
        /// </summary>
        /// <remarks>Used to prevent autoclickers spamming server with individual pickup actions.</remarks>
        public TimeSpan QuickInsertCooldown = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Minimum delay between UI open actions.
        /// <remarks>Used to spamming opening sounds.</remarks>
        /// </summary>
        [DataField]
        public TimeSpan OpenUiCooldown = TimeSpan.Zero;

        /// <summary>
        /// Can insert stuff by clicking the storage entity with it.
        /// </summary>
        [DataField]
        public bool ClickInsert = true;

        /// <summary>
        /// Open the storage window when pressing E.
        /// When false you can still open the inventory using verbs.
        /// </summary>
        [DataField]
        public bool OpenOnActivate = true;

        /// <summary>
        /// How many entities area pickup can pickup at once.
        /// </summary>
        public const int AreaPickupLimit = 10;

        [DataField]
        public bool AreaInsert; // Clicking with the storage entity causes it to insert all nearby storables after a delay

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

        /// <summary>
        /// If true, sets StackVisuals.Hide to true when the container is closed
        /// Used in cases where there are sprites that are shown when the container is open but not
        /// when it is closed
        /// </summary>
        [DataField]
        public bool HideStackVisualsWhenClosed = true;

        /// <summary>
        /// Entities with this tag won't trigger storage sound.
        /// </summary>
        [DataField]
        public ProtoId<TagPrototype> SilentStorageUserTag = "SilentStorageUser";

        [Serializable, NetSerializable]
        public enum StorageUiKey : byte
        {
            Key,
        }

        /// <summary>
        /// Allow or disallow showing the "open/close storage" verb.
        /// This is desired on items that we don't want to be accessed by the player directly.
        /// </summary>
        [DataField]
        public bool ShowVerb = true;
    }

    [Serializable, NetSerializable]
    public sealed class OpenNestedStorageEvent : EntityEventArgs
    {
        public readonly NetEntity InteractedItemUid;
        public readonly NetEntity StorageUid;

        public OpenNestedStorageEvent(NetEntity interactedItemUid, NetEntity storageUid)
        {
            InteractedItemUid = interactedItemUid;
            StorageUid = storageUid;
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
    public sealed class StorageTransferItemEvent : EntityEventArgs
    {
        public readonly NetEntity ItemEnt;

        /// <summary>
        /// Target storage to receive the transfer.
        /// </summary>
        public readonly NetEntity StorageEnt;

        public readonly ItemStorageLocation Location;

        public StorageTransferItemEvent(NetEntity itemEnt, NetEntity storageEnt, ItemStorageLocation location)
        {
            ItemEnt = itemEnt;
            StorageEnt = storageEnt;
            Location = location;
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

    [Serializable, NetSerializable]
    public sealed class StorageSaveItemLocationEvent : EntityEventArgs
    {
        public readonly NetEntity Item;

        public readonly NetEntity Storage;

        public StorageSaveItemLocationEvent(NetEntity item, NetEntity storage)
        {
            Item = item;
            Storage = storage;
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

    [ByRefEvent]
    public record struct StorageInteractAttemptEvent(bool Silent, bool Cancelled = false);

    [ByRefEvent]
    public record struct StorageInteractUsingAttemptEvent(bool Cancelled = false);

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
