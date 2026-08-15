using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// Used for entities that can hold items in different slots. Needed by <see cref="ItemSlotsSystem"/> to support
/// basic insert/eject interactions.
/// </summary>
[RegisterComponent]
[Access(typeof(ItemSlotsSystem))]
[NetworkedComponent]
public sealed partial class ItemSlotsComponent : Component
{
    /// <summary>
    /// The dictionary that stores all of the item slots whose interactions will be managed by the <see
    /// cref="ItemSlotsSystem"/>.
    /// </summary>
    [DataField(readOnly:true)]
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
    // whitelist or whatever), you should use #1.
}

[Serializable, NetSerializable]
public sealed class ItemSlotsComponentState(Dictionary<string, ItemSlot> slots) : ComponentState
{
    public readonly Dictionary<string, ItemSlot> Slots = slots;
}

/// <summary>
/// This is effectively a wrapper for a ContainerSlot that adds content functionality like entity whitelists and
/// insert/eject sounds.
/// </summary>
[DataDefinition]
[Access(typeof(ItemSlotsSystem))]
[Serializable, NetSerializable]
public sealed partial class ItemSlot
{
    public ItemSlot() { }

    [DataField]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    /// <summary>
    /// The name of this item slot. This will be shown to the user in the verb menu.
    /// </summary>
    /// <remarks>
    /// This will be passed through Loc.GetString. If the name is an empty string, then verbs will use the name
    /// of the currently held or currently inserted entity instead.
    /// </remarks>
    [DataField(readOnly: true)]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public string Name = string.Empty;

    /// <summary>
    /// The entity prototype that is spawned into this slot on map init.
    /// </summary>
    /// <remarks>
    /// Marked as readOnly because some components (e.g. PowerCellSlot) set the starting item based on some
    /// property of that component (e.g., cell slot size category), and this can lead to unnecessary changes
    /// when mapping.
    /// </remarks>
    [DataField(readOnly: true)]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    [NonSerialized]
    public EntProtoId? StartingItem;

    /// <summary>
    /// Whether or not an item can currently be ejected or inserted from this slot.
    /// </summary>
    /// <remarks>
    /// This doesn't have to mean the slot is somehow physically locked. In the case of the item cabinet, the
    /// cabinet may simply be closed at the moment and needs to be opened first.
    /// </remarks>
    [DataField(readOnly: true)]
    public bool Locked;

    /// <summary>
    /// Prevents adding the eject alt-verb and ejecting through BUI, but still lets you swap items.
    /// </summary>
    /// <remarks>
    /// This does not affect EjectOnInteract, since if you do that you probably want ejecting to work.
    /// </remarks>
    [DataField]
    public bool DisableEject;

    /// <summary>
    /// Whether the item slots system will attempt to insert item from the user's hands into this slot when interacted with.
    /// It doesn't block other insertion methods, like verbs.
    /// </summary>
    [DataField]
    public bool InsertOnInteract = true;

    /// <summary>
    /// Whether the item slots system will attempt to eject this item to the user's hands when interacted with.
    /// </summary>
    /// <remarks>
    /// For most item slots, this is probably not the case (eject is usually an alt-click interaction). But
    /// there are some exceptions. For example item cabinets and charging stations should probably eject their
    /// contents when clicked on normally.
    /// </remarks>
    [DataField]
    public bool EjectOnInteract;

    /// <summary>
    /// If true, and if this slot is attached to an item, then it will attempt to eject the slot when the item is
    /// used in the user's hands.
    /// </summary>
    /// <remarks>
    /// Desirable for things like ranged weapons ('Z' to eject), but not desirable for others (e.g., PDA uses
    /// 'Z' to open UI). Unlike <see cref="EjectOnInteract"/>, this will not make any changes to the context
    /// menu, nor will it disable alt-click interactions.
    /// </remarks>
    [DataField]
    public bool EjectOnUse;

    /// <summary>
    /// Override the insert verb text. Defaults to using the slot's name (if specified) or the name of the
    /// targeted item. If specified, the verb will not be added to the default insert verb category.
    /// </summary>
    [DataField]
    public string? InsertVerbText;

    /// <summary>
    /// Override the eject verb text. Defaults to using the slot's name (if specified) or the name of the
    /// targeted item. If specified, the verb will not be added to the default eject verb category
    /// </summary>
    [DataField]
    public string? EjectVerbText;

    [ViewVariables, NonSerialized]
    public ContainerSlot? ContainerSlot;

    /// <summary>
    /// If this slot belongs to some de-constructible component, should the item inside the slot be ejected upon
    /// deconstruction?
    /// </summary>
    /// <remarks>
    /// The actual deconstruction logic is handled by the server-side EmptyOnMachineDeconstructSystem.
    /// </remarks>
    [DataField]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)]
    [NonSerialized]
    public bool EjectOnDeconstruct = true;

    /// <summary>
    /// If this slot belongs to some breakable or destructible entity, should the item inside the slot be
    /// ejected when it is broken or destroyed?
    /// </summary>
    [DataField]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)]
    [NonSerialized]
    public bool EjectOnBreak;

    /// <summary>
    /// The popup shown when a standard insertion interaction uses an item rejected by this slot's filters.
    /// </summary>
    [DataField]
    public LocId? WhitelistFailPopup;

    /// <summary>
    /// The popup shown when a standard interaction tries to insert into or eject from this slot while it is locked.
    /// </summary>
    [DataField]
    public LocId? LockedFailPopup;

    /// <summary>
    /// The popup shown after a successful standard insertion interaction, including a swap.
    /// </summary>
    [DataField]
    public LocId? InsertSuccessPopup;

    /// <summary>
    /// Whether insertion interactions may replace the current item after it passes ejection checks.
    /// </summary>
    /// <remarks>
    /// This only affects standard insertion interactions. Direct insertion APIs do not perform slot swapping.
    /// </remarks>
    [DataField]
    [Access(typeof(ItemSlotsSystem), Other = AccessPermissions.ReadWriteExecute)]
    public bool Swap = true;

    public string? ID => ContainerSlot?.ID;

    // Convenience properties
    public bool HasItem => ContainerSlot?.ContainedEntity != null;
    public EntityUid? Item => ContainerSlot?.ContainedEntity;

    /// <summary>
    /// Priority used when selecting and ordering this slot.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Whether this slot originated from local registration rather than received component state.
    /// </summary>
    /// <remarks>
    /// A false value suppresses duplicate-key errors and preserves received configuration when a local component
    /// later registers its corresponding slot.
    /// </remarks>
    [NonSerialized]
    public bool Local = true;
}
