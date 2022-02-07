using System;
using System.Collections.Generic;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Shared.Item
{
    /// <summary>
    ///    Players can pick up, drop, and put items in bags, and they can be seen in player's hands.
    /// </summary>
    [NetworkedComponent()]
    public class SharedItemComponent : Component, IInteractHand
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        ///     How much big this item is.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Size
        {
            get => _size;
            set
            {
                _size = value;
                Dirty();
            }
        }
        [DataField("size")]
        private int _size;

        [DataField("inhandVisuals")]
        public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

        [DataField("clothingVisuals")]
        public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

        /// <summary>
        ///     Part of the state of the sprite shown on the player when this item is in their hands or inventory.
        /// </summary>
        /// <remarks>
        ///     Only used if <see cref="InhandVisuals"/> or <see cref="ClothingVisuals"/> are unspecified.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? EquippedPrefix
        {
            get => _equippedPrefix;
            set
            {
                _equippedPrefix = value;
                EntitySystem.Get<SharedItemSystem>().VisualsChanged(Owner, this);
                Dirty();
            }
        }
        [DataField("HeldPrefix")]
        private string? _equippedPrefix;

        [ViewVariables]
        [DataField("Slots")]
        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

        [DataField("EquipSound")]
        public SoundSpecifier? EquipSound { get; set; } = default!;

        // TODO REMOVE. Currently nonfunctional and only used by RGB system. #6253 Fixes this but requires #6252
        /// <summary>
        ///     Color of the sprite shown on the player when this item is in their hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Dirty();
            }
        }
        [DataField("color")]
        private Color _color = Color.White;

        /// <summary>
        ///     Rsi of the sprite shown on the player when this item is in their hands. Used to generate a default entry for <see cref="InhandVisuals"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sprite")]
        public readonly string? RsiPath;

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            var user = eventArgs.User;

            if (!user.InRangeUnobstructed(Owner, ignoreInsideBlocker: true))
                return false;

            if (!_entMan.TryGetComponent(user, out SharedHandsComponent hands))
                return false;

            var activeHand = hands.ActiveHand;

            if (activeHand == null)
                return false;

            // hands checks action blockers
            return hands.TryPickupEntityToActiveHand(Owner, animateUser: true);
        }

        public void RemovedFromSlot()
        {
            if (_entMan.TryGetComponent(Owner, out SharedSpriteComponent component))
                component.Visible = true;
        }

        public virtual void EquippedToSlot()
        {
            if (_entMan.TryGetComponent(Owner, out SharedSpriteComponent component))
                component.Visible = false;
        }
    }

    [Serializable, NetSerializable]
    public class ItemComponentState : ComponentState
    {
        public int Size { get; }
        public string? EquippedPrefix { get; }

        public ItemComponentState(int size, string? equippedPrefix)
        {
            Size = size;
            EquippedPrefix = equippedPrefix;
        }
    }

    /// <summary>
    ///     Raised when an item's visual state is changed. The event is directed at the entity that contains this item, so
    ///     that it can properly update its hands or inventory sprites and GUI.
    /// </summary>
    [Serializable, NetSerializable]
    public class VisualsChangedEvent : EntityEventArgs
    {
        public readonly EntityUid Item;
        public readonly string ContainerId;

        public VisualsChangedEvent(EntityUid item, string containerId)
        {
            Item = item;
            ContainerId = containerId;
        }
    }

    /// <summary>
    ///     Reference sizes for common containers and items.
    /// </summary>
    public enum ReferenceSizes
    {
        Wallet = 4,
        Pocket = 12,
        Box = 24,
        Belt = 30,
        Toolbox = 60,
        Backpack = 100,
        NoStoring = 9999
    }
}
