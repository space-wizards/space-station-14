using System;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Item
{
    /// <summary>
    ///    Players can pick up, drop, and put items in bags, and they can be seen in player's hands.
    /// </summary>
    [NetworkedComponent()]
    public class SharedItemComponent : Component, IInteractHand
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "Item";

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

        /// <summary>
        ///     Part of the state of the sprite shown on the player when this item is in their hands.
        /// </summary>
        // todo paul make this update slotvisuals on client on change
        [ViewVariables(VVAccess.ReadWrite)]
        public string? EquippedPrefix
        {
            get => _equippedPrefix;
            set
            {
                _equippedPrefix = value;
                OnEquippedPrefixChange();
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
        ///     Rsi of the sprite shown on the player when this item is in their hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? RsiPath
        {
            get => _rsiPath;
            set
            {
                _rsiPath = value;
                Dirty();
            }
        }
        [DataField("sprite")]
        private string? _rsiPath;

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

        private void OnEquippedPrefixChange()
        {
            if (Owner.TryGetContainer(out var container))
                _entMan.EventBus.RaiseLocalEvent(container.Owner, new ItemPrefixChangeEvent(Owner, container.ID));
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
        public Color Color { get; }
        public string? RsiPath { get; }

        public ItemComponentState(int size, string? equippedPrefix, Color color, string? rsiPath)
        {
            Size = size;
            EquippedPrefix = equippedPrefix;
            Color = color;
            RsiPath = rsiPath;
        }
    }

    /// <summary>
    ///     Raised when an item's EquippedPrefix is changed. The event is directed at the entity that contains this item, so
    ///     that it can properly update its sprite/GUI.
    /// </summary>
    [Serializable, NetSerializable]
    public class ItemPrefixChangeEvent : EntityEventArgs
    {
        public readonly EntityUid Item;
        public readonly string ContainerId;

        public ItemPrefixChangeEvent(EntityUid item, string containerId)
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
