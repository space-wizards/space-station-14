#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// This should be used on items which provide actions. Defines which actions the item provides
    /// and allows modifying the states of those actions. Item components should use this rather than
    /// SharedActionsComponent on the player to handle granting / revoking / modifying the states of the
    /// actions provided by this item.
    ///
    /// When a player equips this item, all the actions defined in this component will be granted to the
    /// player in their current states. This means the states will persist between players.
    ///
    /// Currently only maintained server side and not synced to client, as are all the equip/unequip events.
    /// </summary>
    [RegisterComponent]
    public class ItemActionsComponent : Component, IEquippedHand, IEquipped, IUnequipped, IUnequippedHand
    {
        public override string Name => "ItemActions";

        /// <summary>
        /// Configuration for the item actions initially provided by this item. Actions defined here
        /// will be automatically granted unless their state is modified using the methods
        /// on this component. Additional actions can be granted by this item via GrantOrUpdate
        /// </summary>
        public IEnumerable<ItemActionConfig> ActionConfigs => _actionConfigs;

        public bool IsEquipped => InSlot != EquipmentSlotDefines.Slots.NONE || InHand != null;
        /// <summary>
        /// Slot currently equipped to, NONE if not equipped to an equip slot.
        /// </summary>
        public EquipmentSlotDefines.Slots InSlot { get; private set; }
        /// <summary>
        /// hand it's currently in, null if not in a hand.
        /// </summary>
        public HandState? InHand { get; private set; }

        /// <summary>
        /// Entity currently holding this in hand or equip slot. Null if not held.
        /// </summary>
        public IEntity? Holder { get; private set; }
        // cached actions component of the holder, since we'll need to access it frequently
        private SharedActionsComponent? _holderActionsComponent;

        [DataField("actions")]
        private List<ItemActionConfig> _actionConfigs
        {
            get => internalActionConfigs;
            set
            {
                internalActionConfigs = value;
                foreach (var actionConfig in value)
                {
                    GrantOrUpdate(actionConfig.ActionType, actionConfig.Enabled, false, null);
                }
            }
        }

        // State of all actions provided by this item.
        private readonly Dictionary<ItemActionType, ActionState> _actions = new();
        private List<ItemActionConfig> internalActionConfigs = new ();

        protected override void Startup()
        {
            base.Startup();
            GrantOrUpdateAllToHolder();
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            RevokeAllFromHolder();
        }

        private void GrantOrUpdateAllToHolder()
        {
            if (_holderActionsComponent == null) return;
            foreach (var (actionType, state) in _actions)
            {
                _holderActionsComponent.GrantOrUpdateItemAction(actionType, Owner.Uid, state);
            }
        }

        private void RevokeAllFromHolder()
        {
            if (_holderActionsComponent == null) return;
            foreach (var (actionType, state) in _actions)
            {
                _holderActionsComponent.RevokeItemAction(actionType, Owner.Uid);
            }
        }

        /// <summary>
        /// Update the state of the action, granting it if it isn't already granted.
        /// If the action had any existing state, those specific fields will be overwritten by any
        /// corresponding non-null arguments.
        /// </summary>
        /// <param name="actionType">action being granted / updated</param>
        /// <param name="enabled">When null, preserves the current enable status of the action, defaulting
        /// to true if action has no current state.
        /// When non-null, indicates whether the entity is able to perform the action (if disabled,
        /// the player will see they have the action but it will appear greyed out)</param>
        /// <param name="toggleOn">When null, preserves the current toggle status of the action, defaulting
        /// to false if action has no current state.
        /// When non-null, action will be shown toggled to this value</param>
        /// <param name="cooldown"> When null (unless clearCooldown is true), preserves the current cooldown status of the action, defaulting
        /// to no cooldown if action has no current state.
        /// When non-null or clearCooldown is true, action cooldown will be set to this value. Note that this cooldown
        /// is tied to this item.</param>
        /// <param name="clearCooldown"> If true, setting cooldown to null will clear the current cooldown
        /// of this action rather than preserving it.</param>
        public void GrantOrUpdate(ItemActionType actionType, bool? enabled = null,
            bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null, bool clearCooldown = false)
        {
            var dirty = false;
            // this will be overwritten if we find the value in our dict, otherwise
            // we will use this as our new action state.
            if (!_actions.TryGetValue(actionType, out var actionState))
            {
                dirty = true;
                actionState = new ActionState(enabled ?? true, toggleOn ?? false);
            }

            if (enabled.HasValue && enabled != actionState.Enabled)
            {
                dirty = true;
                actionState.Enabled = true;
            }

            if ((cooldown.HasValue || clearCooldown) && actionState.Cooldown != cooldown)
            {
                dirty = true;
                actionState.Cooldown = cooldown;
            }

            if (toggleOn.HasValue && actionState.ToggledOn != toggleOn.Value)
            {
                dirty = true;
                actionState.ToggledOn = toggleOn.Value;
            }

            if (!dirty) return;

            _actions[actionType] = actionState;
            _holderActionsComponent?.GrantOrUpdateItemAction(actionType, Owner.Uid, actionState);
        }

        /// <summary>
        /// Update the cooldown of a particular action. Actions on cooldown cannot be used.
        /// Setting the cooldown to null clears it.
        /// </summary>
        public void Cooldown(ItemActionType actionType, (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            GrantOrUpdate(actionType, cooldown: cooldown, clearCooldown: true);
        }

        /// <summary>
        /// Enable / disable this action. Disabled actions are still shown to the player, but
        /// shown as not usable.
        /// </summary>
        public void SetEnabled(ItemActionType actionType, bool enabled)
        {
            GrantOrUpdate(actionType, enabled);
        }

        /// <summary>
        /// Toggle the action on / off
        /// </summary>
        public void Toggle(ItemActionType actionType, bool toggleOn)
        {
            GrantOrUpdate(actionType, toggleOn: toggleOn);
        }

        void IEquippedHand.EquippedHand(EquippedHandEventArgs eventArgs)
        {
            // this entity cannot be granted actions if no actions component
            if (!eventArgs.User.TryGetComponent<SharedActionsComponent>(out var actionsComponent))
                return;
            Holder = eventArgs.User;
            _holderActionsComponent = actionsComponent;
            InSlot = EquipmentSlotDefines.Slots.NONE;
            InHand = eventArgs.Hand;
            GrantOrUpdateAllToHolder();
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            // this entity cannot be granted actions if no actions component
            if (!eventArgs.User.TryGetComponent<SharedActionsComponent>(out var actionsComponent))
                return;
            Holder = eventArgs.User;
            _holderActionsComponent = actionsComponent;
            InSlot = eventArgs.Slot;
            InHand = null;
            GrantOrUpdateAllToHolder();
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            RevokeAllFromHolder();
            Holder = null;
            _holderActionsComponent = null;
            InSlot = EquipmentSlotDefines.Slots.NONE;
            InHand = null;

        }

        void IUnequippedHand.UnequippedHand(UnequippedHandEventArgs eventArgs)
        {
            RevokeAllFromHolder();
            Holder = null;
            _holderActionsComponent = null;
            InSlot = EquipmentSlotDefines.Slots.NONE;
            InHand = null;
        }
    }

    /// <summary>
    /// Configuration for an item action provided by an item.
    /// </summary>
    [DataDefinition]
    public class ItemActionConfig : ISerializationHooks
    {
        [DataField("actionType", required: true)]
        public ItemActionType ActionType { get; private set; } = ItemActionType.Error;

        /// <summary>
        /// Whether action is initially enabled on this item. Defaults to true.
        /// </summary>
        public bool Enabled { get; private set; } = true;

        void ISerializationHooks.AfterDeserialization()
        {
            if (ActionType == ItemActionType.Error)
            {
                Logger.ErrorS("action", "invalid or missing actionType");
            }
        }
    }
}
