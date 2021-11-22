using Content.Shared.Sound;
using Content.Shared.Whitelist;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Shared.Containers.ItemSlots
{
    /// <summary>
    ///     Used for entities that can hold items in different slots. Needed by ItemSlotSystem to support basic
    ///     insert/eject interactions.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(ItemSlotsSystem))]
    public class ItemSlotsComponent : Component
    {
        public override string Name => "ItemSlots";

        /// <summary>
        ///     The dictionary that stores all of the item slots whose interactions will be managed by the <see
        ///     cref="ItemSlotsSystem"/>.
        /// </summary>
        [DataField("slots", readOnly:true)]
        public readonly Dictionary<string, ItemSlot> Slots = new();

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
        public readonly Dictionary<string, bool> SlotLocked;

        public ItemSlotsComponentState(Dictionary<string, ItemSlot> slots)
        {
            SlotLocked = new(slots.Count);

            foreach (var (key, slot) in slots)
            {
                SlotLocked[key] = slot.Locked;
            }
        }
    }

    /// <summary>
    ///     This is effectively a wrapper for a ContainerSlot that adds content functionality like entity whitelists and
    ///     insert/eject sounds.
    /// </summary>
    [DataDefinition]
    [Friend(typeof(ItemSlotsSystem))]
    public class ItemSlot
    {
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        [DataField("insertSound")]
        public SoundSpecifier? InsertSound;
        // maybe default to /Audio/Weapons/Guns/MagIn/batrifle_magin.ogg ??

        [DataField("ejectSound")]
        public SoundSpecifier? EjectSound;
        // maybe default to /Audio/Machines/id_swipe.ogg?

        /// <summary>
        ///     The name of this item slot. This will be shown to the user in the verb menu.
        /// </summary>
        /// <remarks>
        ///     This will be passed through Loc.GetString. If the name is an empty string, then verbs will use the name
        ///     of the currently held or currently inserted entity instead.
        /// </remarks>
        [DataField("name")]
        public string Name = string.Empty;

        [DataField("startingItem", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? StartingItem;

        /// <summary>
        ///     Whether or not an item can currently be ejected or inserted from this slot.
        /// </summary>
        /// <remarks>
        ///     This doesn't have to mean the slot is somehow physically locked. In the case of the item cabinet, the
        ///     cabinet may simply be closed at the moment and needs to be opened first.
        /// </remarks>
        [DataField("locked")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Locked = false;

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
        ///     Override the insert verb text. Defaults to [insert category] -> [item-name]. If not null, the verb will
        ///     not be given a category.
        /// </summary>
        [DataField("insertVerbText")]
        public string? InsertVerbText;

        /// <summary>
        ///     Override the insert verb text. Defaults to [eject category] -> [item-name]. If not null, the verb will
        ///     not be given a category.
        /// </summary>
        [DataField("ejectVerbText")]
        public string? EjectVerbText;

        [ViewVariables]
        public ContainerSlot ContainerSlot = default!;

        public string ID => ContainerSlot.ID;

        // Convenience properties
        public bool HasItem => ContainerSlot.ContainedEntity != null;
        public IEntity? Item => ContainerSlot.ContainedEntity;
    }
}
