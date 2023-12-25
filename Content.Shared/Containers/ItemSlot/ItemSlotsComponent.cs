using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Containers.ItemSlots
{
    /// <summary>
    ///     Used for entities that can hold items in different slots. Needed by ItemSlotSystem to support basic
    ///     insert/eject interactions.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ItemSlotsSystem))]
    [NetworkedComponent]
    public sealed partial class ItemSlotsComponent : Component
    {
        /// <summary>
        ///     The dictionary that stores all of the item slots whose interactions will be managed by the <see
        ///     cref="ItemSlotsSystem"/>.
        /// </summary>
        [DataField("slots", readOnly:true)]
        public Dictionary<string, ItemSlot> Slots = new();

        // There are two ways to use item slots:
        //
        // #1 - Give your component an ItemSlot datafield, and add/remove the item slot through the ItemSlotsSystem on
        // component init/remove.
        //
        // #2 - Give your component a key string datafield, and make sure that every entity with that component also has
        // an ItemSlots component with a matching key. Then use ItemSlots system to get the slot with this key whenever
        // you need it, or just get a reference to the slot on init and store it. This is how generic entity containers
        // are usually used.
        //
        // In order to avoid #1 leading to duplicate slots when saving a map, the Slots dictionary is a read-only
        // datafield. This means that if your system/component dynamically changes the item slot (e.g., updating
        // whitelist or whatever), you should use #1. Alternatively: split the Slots dictionary here into two: one
        // datafield, one that is actually used by the ItemSlotsSystem for keeping track of slots.
    }

    [Serializable, NetSerializable]
    public sealed class ItemSlotsComponentState : ComponentState
    {
        public readonly Dictionary<string, ItemSlot> Slots;

        public ItemSlotsComponentState(Dictionary<string, ItemSlot> slots)
        {
            Slots = slots;
        }
    }

    /// <summary>
    ///     This is effectively a wrapper for a ContainerSlot that adds content functionality like entity whitelists and
    ///     insert/eject sounds.
    /// </summary>
    [DataDefinition]
    [Access(typeof(ItemSlotsSystem))]
    [Serializable, NetSerializable]
    public sealed partial class ItemSlot
    {
        public ItemSlot() { }

        public ItemSlot(ItemSlot other)
        {
            CopyFrom(other);
        }


        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        [DataField("blacklist")]
        public EntityWhitelist? Blacklist;

        [DataField("insertSound")]
        public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

        [DataField("ejectSound")]
        public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

        /// <summary>
        ///     The name of this item slot. This will be shown to the user in the verb menu.
        /// </summary>
        /// <remarks>
        ///     This will be passed through Loc.GetString. If the name is an empty string, then verbs will use the name
        ///     of the currently held or currently inserted entity instead.
        /// </remarks>
        [DataField("name", readOnly: true)]
        [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public string Name = string.Empty;

        /// <summary>
        ///     The entity prototype that is spawned into this slot on map init.
        /// </summary>
        /// <remarks>
        ///     Marked as readOnly because some components (e.g. PowerCellSlot) set the starting item based on some
        ///     property of that component (e.g., cell slot size category), and this can lead to unnecessary changes
        ///     when mapping.
        /// </remarks>
        [DataField("startingItem", readOnly: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        [NonSerialized]
        public string? StartingItem;

        /// <summary>
        ///     Whether or not an item can currently be ejected or inserted from this slot.
        /// </summary>
        /// <remarks>
        ///     This doesn't have to mean the slot is somehow physically locked. In the case of the item cabinet, the
        ///     cabinet may simply be closed at the moment and needs to be opened first.
        /// </remarks>
        [DataField("locked", readOnly: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Locked = false;

        /// <summary>
        /// Prevents adding the eject alt-verb, but still lets you swap items.
        /// </summary>
        /// <remarks>
        ///     This does not affect EjectOnInteract, since if you do that you probably want ejecting to work.
        /// </remarks>
        [DataField("disableEject"), ViewVariables(VVAccess.ReadWrite)]
        public bool DisableEject = false;

        /// <summary>
        ///     Whether the item slots system will attempt to insert item from the user's hands into this slot when interacted with.
        ///     It doesn't block other insertion methods, like verbs.
        /// </summary>
        [DataField("insertOnInteract")]
        public bool InsertOnInteract = true;

        /// <summary>
        ///     Whether the item slots system will attempt to eject this item to the user's hands when interacted with.
        /// </summary>
        /// <remarks>
        ///     For most item slots, this is probably not the case (eject is usually an alt-click interaction). But
        ///     there are some exceptions. For example item cabinets and charging stations should probably eject their
        ///     contents when clicked on normally.
        /// </remarks>
        [DataField("ejectOnInteract")]
        public bool EjectOnInteract = false;

        /// <summary>
        ///     If true, and if this slot is attached to an item, then it will attempt to eject slot when to the slot is
        ///     used in the user's hands.
        /// </summary>
        /// <remarks>
        ///     Desirable for things like ranged weapons ('Z' to eject), but not desirable for others (e.g., PDA uses
        ///     'Z' to open UI). Unlike <see cref="EjectOnInteract"/>, this will not make any changes to the context
        ///     menu, nor will it disable alt-click interactions.
        /// </remarks>
        [DataField("ejectOnUse")]
        public bool EjectOnUse = false;

        /// <summary>
        ///     Override the insert verb text. Defaults to using the slot's name (if specified) or the name of the
        ///     targeted item. If specified, the verb will not be added to the default insert verb category.
        /// </summary>
        [DataField("insertVerbText")]
        public string? InsertVerbText;

        /// <summary>
        ///     Override the eject verb text. Defaults to using the slot's name (if specified) or the name of the
        ///     targeted item. If specified, the verb will not be added to the default eject verb category
        /// </summary>
        [DataField("ejectVerbText")]
        public string? EjectVerbText;

        [ViewVariables, NonSerialized]
        public ContainerSlot? ContainerSlot = default!;

        /// <summary>
        ///     If this slot belongs to some de-constructible component, should the item inside the slot be ejected upon
        ///     deconstruction?
        /// </summary>
        /// <remarks>
        ///     The actual deconstruction logic is handled by the server-side EmptyOnMachineDeconstructSystem.
        /// </remarks>
        [DataField("ejectOnDeconstruct")]
        [NonSerialized]
        public bool EjectOnDeconstruct = true;

        /// <summary>
        ///     If this slot belongs to some breakable or destructible entity, should the item inside the slot be
        ///     ejected when it is broken or destroyed?
        /// </summary>
        [DataField("ejectOnBreak")]
        [NonSerialized]
        public bool EjectOnBreak = false;

        /// <summary>
        ///     If this is not an empty string, this will generate a popup when someone attempts to insert a bad item
        ///     into this slot. This string will be passed through localization.
        /// </summary>
        [DataField("whitelistFailPopup")]
        public string WhitelistFailPopup = string.Empty;

        /// <summary>
        ///     If the user interacts with an entity with an already-filled item slot, should they attempt to swap out the item?
        /// </summary>
        /// <remarks>
        ///     Useful for things like chem dispensers, but undesirable for things like the ID card console, where you
        ///     want to insert more than one item that matches the same whitelist.
        /// </remarks>
        [DataField("swap")]
        public bool Swap = true;

        public string? ID => ContainerSlot?.ID;

        // Convenience properties
        public bool HasItem => ContainerSlot?.ContainedEntity != null;
        public EntityUid? Item => ContainerSlot?.ContainedEntity;

        /// <summary>
        ///     Priority for use with the eject & insert verbs for this slot.
        /// </summary>
        [DataField("priority")]
        public int Priority = 0;

        /// <summary>
        ///     If false, errors when adding an item slot with a duplicate key are suppressed. Local==true implies that
        ///     the slot was added via client component state handling.
        /// </summary>
        [NonSerialized]
        public bool Local = true;

        public void CopyFrom(ItemSlot other)
        {
            // These fields are mutable reference types. But they generally don't get modified, so this should be fine.
            Whitelist = other.Whitelist;
            InsertSound = other.InsertSound;
            EjectSound = other.EjectSound;

            Name = other.Name;
            Locked = other.Locked;
            InsertOnInteract = other.InsertOnInteract;
            EjectOnInteract = other.EjectOnInteract;
            EjectOnUse = other.EjectOnUse;
            InsertVerbText = other.InsertVerbText;
            EjectVerbText = other.EjectVerbText;
            WhitelistFailPopup = other.WhitelistFailPopup;
            Swap = other.Swap;
            Priority = other.Priority;
        }
    }

    /// <summary>
    /// Event raised on the slot entity and the item being inserted to determine if an item can be inserted into an item slot.
    /// </summary>
    [ByRefEvent]
    public record struct ItemSlotInsertAttemptEvent(EntityUid SlotEntity, EntityUid Item, EntityUid? User, ItemSlot Slot, bool Cancelled = false);

    /// <summary>
    /// Event raised on the slot entity and the item being inserted to determine if an item can be ejected from an item slot.
    /// </summary>
    [ByRefEvent]
    public record struct ItemSlotEjectAttemptEvent(EntityUid SlotEntity, EntityUid Item, EntityUid? User, ItemSlot Slot, bool Cancelled = false);
}
