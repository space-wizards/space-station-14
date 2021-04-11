#nullable enable
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Shared.GameObjects.Components.Storage
{
    /// <summary>
    ///    Players can pick up, drop, and put items in bags, and they can be seen in player's hands.
    /// </summary>
    public abstract class SharedItemComponent : Component, IEquipped, IUnequipped, IInteractHand
    {
        public override string Name => "Item";

        public override uint? NetID => ContentNetIDs.ITEM;

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

        /// <summary>
        ///     Color of the sprite shown on the player when this item is in their hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => _color;
            protected set
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

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ItemComponentState(Size, EquippedPrefix, Color, RsiPath);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not ItemComponentState state)
                return;

            Size = state.Size;
            EquippedPrefix = state.EquippedPrefix;
            Color = state.Color;
            RsiPath = state.RsiPath;
        }

        /// <summary>
        ///     If a player can pick up this item.
        /// </summary>
        public bool CanPickup(IEntity user)
        {
            if (!ActionBlockerSystem.CanPickup(user))
                return false;

            if (user.Transform.MapID != Owner.Transform.MapID)
                return false;

            if (!Owner.TryGetComponent(out IPhysBody? physics) || physics.BodyType == BodyType.Static)
                return false;

            return user.InRangeUnobstructed(Owner, ignoreInsideBlocker: true, popup: true);
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            EquippedToSlot();
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            RemovedFromSlot();
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            var user = eventArgs.User;

            if (!CanPickup(user))
                return false;

            if (!user.TryGetComponent(out SharedHandsComponent? hands))
                return false;

            var activeHand = hands.ActiveHand;

            if (activeHand == null)
                return false;

            hands.TryPickupEntityToActiveHand(Owner);
            return true;
        }

        protected virtual void OnEquippedPrefixChange() { }

        public virtual void RemovedFromSlot() { }

        public virtual void EquippedToSlot() { }
    }

    [Serializable, NetSerializable]
    public class ItemComponentState : ComponentState
    {
        public int Size { get; }
        public string? EquippedPrefix { get; }
        public Color Color { get; }
        public string? RsiPath { get; }

        public ItemComponentState(int size, string? equippedPrefix, Color color, string? rsiPath) : base(ContentNetIDs.ITEM)
        {
            Size = size;
            EquippedPrefix = equippedPrefix;
            Color = color;
            RsiPath = rsiPath;
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
