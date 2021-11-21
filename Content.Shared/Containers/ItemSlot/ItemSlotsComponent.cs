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

        [ViewVariables]
        [DataField("slots")]
        public Dictionary<string, ItemSlot> Slots = new();
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
