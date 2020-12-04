using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Manages the actions available to an entity.
    /// Should only be used for player-controlled entities.
    ///
    /// Actions are granted directly to the owner entity. Item actions are granted via a particular item which
    /// must be in the owner's inventory (the action is revoked when item leaves the owner's inventory).
    ///
    /// Actions can still have an associated state even when revoked. For example, a flashlight toggle action
    /// may be unusable while the player is stunned, but this component will still have an entry for the action
    /// so the user can see whether it's currently toggled on or off.
    ///
    /// Note that cooldowns in both cases are tracked on the owner, not the item. Dropping and picking up an
    /// item which has item actions on a cooldown will preserve the cooldowns only for that player. If another
    /// player picks up the item, this new player will have no cooldowns. Eventually we may want
    /// some item actions tracked on the item itself.
    /// </summary>
    public abstract class SharedActionsComponent : Component
    {
        private static readonly TimeSpan CooldownExpiryThreshold = TimeSpan.FromSeconds(10);

        private static readonly ActionState[] NoActions = new ActionState[0];

        [Dependency]
        protected readonly ActionManager ActionManager = default!;
        [Dependency]
        protected readonly IGameTiming GameTiming = default!;
        [Dependency]
        protected readonly IEntityManager EntityManager = default!;
        private SharedHandsComponent _handsComponent;
        private SharedInventoryComponent _inventoryComponent;

        public override string Name => "ActionsUI";
        public override uint? NetID => ContentNetIDs.ACTIONS;


        // entries are removed from this if they are at the initial state (not enabled, no cooldown, toggled off).
        // a system runs which periodically removes cooldowns from entries when they are revoked and their
        // cooldowns have expired for a long enough time, also removing the entry if it is then at initial state.
        // This helps to keep our component state smaller.
        [ViewVariables]
        private Dictionary<ActionType, ActionState> _actions = new Dictionary<ActionType, ActionState>();

        // Holds item action states. Item actions are only added to this when granted, and are removed
        // when revoked or when they leave inventory. If they had a cooldown when revoked,
        // those cooldowns are tracked in _itemActionCooldowns data structure to be restored in the event the item
        // re-enters their inventory. This ensures a player can't clear their cooldowns by dropping and picking up an item.
        // We also ensure the values in this dictionary are never empty.
        [ViewVariables]
        private Dictionary<EntityUid, Dictionary<ItemActionType, ActionState>> _itemActions =
            new Dictionary<EntityUid, Dictionary<ItemActionType, ActionState>>();

        // this holds cooldowns for revoked item actions that had a cooldown when they were revoked.
        // they are used to restore cooldowns when the item re-enters inventory
        // these are not part of component state and thus not synced to the client.
        // A system runs periodically to evict entries from this when their cooldowns have expired for a long enough time.
        private Dictionary<(EntityUid item, ItemActionType actionType), (TimeSpan start, TimeSpan end)> _itemActionCooldowns =
            new Dictionary<(EntityUid item, ItemActionType actionType), (TimeSpan start, TimeSpan end)>();

        public override void Initialize()
        {
            base.Initialize();
            _handsComponent = Owner.GetComponent<SharedHandsComponent>();
            _inventoryComponent = Owner.GetComponent<SharedInventoryComponent>();
        }

        public override ComponentState GetComponentState()
        {
            return new ActionComponentState(_actions, _itemActions);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is ActionComponentState state))
            {
                return;
            }
            _actions = state.Actions;
            _itemActions = state.ItemActions;
        }

        /// <summary>
        /// Gets the action state associated with the specified action type, if it has been
        /// granted, has a cooldown, or has been toggled on
        /// </summary>
        /// <returns>false if not found for this action type</returns>
        public bool TryGetActionState(ActionType actionType, out ActionState actionState)
        {
            return _actions.TryGetValue(actionType, out actionState);
        }

        /// <summary>
        /// Gets the item action states associated with the specified item if any have been granted
        /// and not yet revoked.
        /// </summary>
        /// <returns>false if no states found for this item action type.</returns>
        public bool TryGetItemActionStates(EntityUid item, out IReadOnlyDictionary<ItemActionType, ActionState> itemActionStates)
        {
            if (_itemActions.TryGetValue(item, out var actualItemActionStates))
            {
                itemActionStates = actualItemActionStates;
                return true;
            }

            itemActionStates = null;
            return false;
        }

        /// <see cref="TryGetItemActionStates"/>
        public bool TryGetItemActionStates(IEntity item,
            out IReadOnlyDictionary<ItemActionType, ActionState> itemActionStates)
        {
            return TryGetItemActionStates(item.Uid, out itemActionStates);
        }

        /// <summary>
        /// Gets the item action state associated with the specified item action type for the specified item, if it has any.
        /// </summary>
        /// <returns>false if no state found for this item action type for this item</returns>
        public bool TryGetItemActionState(ItemActionType actionType, EntityUid item, out ActionState actionState)
        {
            if (_itemActions.TryGetValue(item, out var actualItemActionStates))
            {
                return actualItemActionStates.TryGetValue(actionType, out actionState);
            }

            actionState = default;
            return false;
        }

        public bool IsGranted(ActionType actionType)
        {
            if (TryGetActionState(actionType, out var actionState))
            {
                return actionState.Enabled;
            }

            return false;
        }

        /// <returns>true if the action is granted and enabled for any item. This
        /// has to traverse the entire item state dictionary so please avoid frequent calls.</returns>
        public bool IsGranted(ItemActionType actionType)
        {
            return _itemActions.Values.SelectMany(vals => vals)
                .Any(state => state.Key == actionType && state.Value.Enabled);
        }

        /// <see cref="TryGetItemActionState"/>
        public bool TryGetItemActionState(ItemActionType actionType, IEntity item, out ActionState actionState)
        {
            return TryGetItemActionState(actionType, item.Uid, out actionState);
        }

        /// <summary>
        /// Gets all action types that have non-initial state (granted, have a cooldown, or toggled on).
        /// </summary>
        protected IEnumerable<KeyValuePair<ActionType, ActionState>> EnumerateActionStates()
        {
            return _actions;
        }

        /// <summary>
        /// Gets all items that have actions currently granted (that are not revoked
        /// and still in inventory).
        /// </summary>
        protected IEnumerable<KeyValuePair<EntityUid,Dictionary<ItemActionType, ActionState>>> EnumerateItemActions()
        {
            return _itemActions;
        }

        /// <summary>
        /// Creates or updates the action state with the supplied non-null values
        /// </summary>
        private void CreateOrUpdate(ActionType actionType, bool? enabled = null, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            var dirty = false;
            if (!_actions.TryGetValue(actionType, out var actionState))
            {
                // no state at all for this action, create it anew
                dirty = true;
                actionState = new ActionState(enabled ?? false, toggleOn ?? false);
            }

            if (enabled.HasValue && actionState.Enabled != enabled.Value)
            {
                dirty = true;
                actionState.Enabled = enabled.Value;
            }

            if (cooldown.HasValue && actionState.Cooldown != cooldown)
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
            AfterActionChanged();
            Dirty();
        }

        /// <summary>
        /// Updates the action state with the supplied non-null values, only creating it if it doesn't
        /// exist and we are explicitly allowed to create it
        /// </summary>
         private void CreateOrUpdate(ItemActionType actionType, EntityUid item, bool createIfNeeded, bool? enabled = null, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            if (!IsEquipped(item))
            {
                Logger.WarningS("action", "cannot grant item action {0} to {1} for item {2} " +
                                          " because it is not in the owner's inventory", actionType, Owner.Name, item);
                return;
            }

            var dirty = false;
            // this will be overwritten if we find the value in our dict, otherwise
            // we will use this as our new action state.
            var actionState = new ActionState(enabled ?? true, toggleOn ?? false);
            // retrieve cooldown for the above state if we had a saved one (this will only be used if there
            // was no existing action state)
            if (_itemActionCooldowns.Remove((item, actionType), out var savedCooldown))
            {
                // we no longer need it to be saved since we are granting this action.
                actionState.Cooldown = savedCooldown;
            }

            if (_itemActions.TryGetValue(item, out var itemStates))
            {
                // we already have some states for this item
                // look up the state for this type, creating it if we don't have one
                if (!itemStates.TryGetValue(actionType, out actionState))
                {
                    // didn't exist for this item, we will use the state created above
                    if (!createIfNeeded) return;
                    dirty = true;
                }
            }
            else
            {
                // no state at all for this item action, create it anew as well as the state
                if (!createIfNeeded) return;
                dirty = true;
                itemStates = new Dictionary<ItemActionType, ActionState>();
            }

            if (enabled.HasValue && enabled != actionState.Enabled)
            {
                dirty = true;
                actionState.Enabled = true;
            }

            if (cooldown.HasValue && actionState.Cooldown != cooldown)
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

            itemStates[actionType] = actionState;
            _itemActions[item] = itemStates;
            AfterActionChanged();
            Dirty();
        }

        /// <summary>
        /// Updates the action state with the supplied non-null values, only creating it if it doesn't
        /// exist and we are explicitly allowed to create it
        /// </summary>
        private void CreateOrUpdate(ItemActionType actionType, IEntity item, bool createIfNeeded,
            bool? enabled = null, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            if (IsNullItem(item)) return;
            CreateOrUpdate(actionType, item.Uid, createIfNeeded, enabled, toggleOn, cooldown);
        }

        /// <summary>
        /// Grants the entity the ability to perform the action, optionally overriding its
        /// current state with specified values.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be preserved, with specific fields optionally overridden by any of the provided
        /// non-null arguments.
        /// </summary>
        /// <param name="toggleOn">When null, preserves the current toggle status of the action, defaulting
        /// to false if action has no current state.
        /// When non-null, action will be shown toggled to this value</param>
        /// <param name="cooldown"> When null, preserves the current cooldown status of the action, defaulting
        /// to no cooldown if action has no current state.
        /// When non-null, action cooldown will be set to this value.</param>
        public void Grant(ActionType actionType, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
           CreateOrUpdate(actionType, true, toggleOn, cooldown);
        }

        /// <summary>
        /// Grants the entity the knowledge of the item action for the specified item in their inventory, optionally overriding its
        /// current state with specified values. No effect if item is not in inventory. When the item is removed
        /// from inventory, this action will be automatically revoked.
        ///
        /// Unlike a normal action, this does not necessarily allow
        /// the entity to perform the action, unless enabled = true. For example, you may want to set enabled = false
        /// if the item has the ability to perform the action but there is something temporarily preventing
        /// it from being performed. This way the player knows the item has the action but they must do something
        /// in order to be able to enable it.
        ///
        /// Even if the action was already granted, if the action had any state (enabled, cooldown, toggle) prior to this method
        /// being called, it will be preserved, with specific fields optionally overridden by any of the provided
        /// non-null arguments.
        /// </summary>
        /// <param name="enabled">When null, preserves the current enable status of the action, defaulting
        /// to true if action has no current state.
        /// When non-null, indicates whether the entity is able to perform the action</param>
        /// <param name="toggleOn">When null, preserves the current toggle status of the action, defaulting
        /// to false if action has no current state.
        /// When non-null, action will be shown toggled to this value</param>
        /// <param name="cooldown"> When null, preserves the current cooldown status of the action, defaulting
        /// to no cooldown if action has no current state.
        /// When non-null, action cooldown will be set to this value. Note that this cooldown
        /// is tracked on the owner. If the player drops and picks up the item
        /// they will still have the cooldown, but if another player picks up the item that other player
        /// will see no cooldown.</param>
        public void Grant(ItemActionType actionType, IEntity item, bool? enabled = null, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            CreateOrUpdate(actionType, item, true, enabled, toggleOn, cooldown);
        }

        /// <summary>
        /// Grants the entity the ability to perform the action, resetting its state
        /// to its initial state and settings its state based on supplied parameters.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be reset to initial (no cooldown, toggled off).
        /// </summary>
        /// <param name="toggleOn">action will be shown toggled to this value</param>
        /// <param name="cooldown">action cooldown will be set to this value (by default the cooldown is cleared).</param>
        public void GrantFromInitialState(ActionType actionType, bool toggleOn = false,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            _actions.Remove(actionType);
            Grant(actionType, toggleOn, cooldown);
        }

        /// <summary>
        /// Grants the entity the knowledge of the item action for the specified item in their inventory, resetting its state
        /// to its initial state and settings its state based on supplied parameters. No effect if item is not in inventory.
        /// When the item is removed from inventory, this action will be automatically revoked.
        ///
        /// Unlike a normal action, this does not necessarily allow
        /// the entity to perform the action, unless enabled = true. For example, you may want to set enabled = false
        /// if the item has the ability to perform the action but there is something temporarily preventing
        /// it from being performed. This way the player knows the item has the action but they must do something
        /// in order to be able to enable it.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be reset to initial (no cooldown, toggled off).
        /// </summary>
        /// <param name="enabled">whether the entity is able to perform the action</param>
        /// <param name="toggleOn">action will be shown toggled to this value</param>
        /// <param name="cooldown">action cooldown will be set to this value (by default the cooldown is cleared).
        /// Note that this cooldown is tracked on the owner.
        /// If the player drops and picks up the item
        /// they will still have the cooldown, but if another player picks up the item that other player
        /// will see no cooldown.</param>
        public void GrantFromInitialState(ItemActionType actionType, IEntity item, bool enabled = true, bool toggleOn = false,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            if (IsNullItem(item)) return;
            if (_itemActions.TryGetValue(item.Uid, out var itemStates))
            {
                itemStates.Remove(actionType);
                if (itemStates.Count == 0)
                {
                    _itemActions.Remove(item.Uid);
                }
            }
            Grant(actionType, item, enabled, toggleOn, cooldown);
        }

        /// <summary>
        /// Sets the cooldown for the action. Actions on cooldown cannot be used.
        ///
        /// This will work even if the action is revoked -
        /// for example if there's an ability with a cooldown which is temporarily unusable due
        /// to the player being stunned, the cooldown will still tick down even while the player
        /// is stunned.
        ///
        /// Setting cooldown to null clears it.
        /// </summary>
        public void Cooldown(ActionType actionType, (TimeSpan start, TimeSpan end)? cooldown)
        {
            CreateOrUpdate(actionType, cooldown: cooldown);
        }

        /// <summary>
        /// Sets the cooldown for the item action. This will work even if
        /// the item is revoked or not in inventory. Actions on cooldown cannot be used.
        ///
        /// Note that this cooldown is tracked on the owner.
        /// If the player drops and picks up the item
        /// they will still have the cooldown, but if another player picks up the item that other player
        /// will see no cooldown.
        ///
        /// This will work even if the action is not enabled -
        /// for example if there's an item action with a cooldown which is temporarily unusable due
        /// to the player being stunned, the cooldown will still tick down even while the player
        /// is stunned.
        ///
        /// Setting cooldown to null clears it.
        /// </summary>
        public void Cooldown(ItemActionType actionType, EntityUid item, (TimeSpan start, TimeSpan end)? cooldown)
        {
            // if it's not here it's revoked or not in inventory, simply save the cooldown for when the
            // action is later granted
            if (!TryGetItemActionState(actionType, item, out var actionState))
            {
                if (_itemActionCooldowns.TryGetValue((item, actionType), out var savedCooldown))
                {
                    cooldown = savedCooldown;
                }

                if (!cooldown.HasValue)
                {
                    // clear existing cooldown (we don't save nulls in there so simply remove the entry)
                    _itemActionCooldowns.Remove((item, actionType));
                }
                else
                {
                    _itemActionCooldowns[(item, actionType)] = cooldown.Value;
                }

                return;
            }

            actionState.Cooldown = cooldown;
            // note we can be sure the below keys exist, due to the above call to TryGetItemActionState
            _itemActions[item][actionType] = actionState;
            AfterActionChanged();
            Dirty();
        }

        /// <see cref="Cooldown"/>
        public void Cooldown(ItemActionType actionType, IEntity item, (TimeSpan start, TimeSpan end)? cooldown)
        {
            if (IsNullItem(item)) return;
            Cooldown(actionType, item.Uid, cooldown);
        }

        /// <summary>
        /// Enables the entity to use the action for this item. No effect if the action
        /// has not yet been granted, is revoked, or item is not in inventory.
        /// </summary>
        public void SetEnabled(ItemActionType actionType, EntityUid item, bool enabled)
        {
            CreateOrUpdate(actionType, item, false, enabled);
        }

        /// <see cref="SetEnabled"/>
        public void SetEnabled(ItemActionType actionType, IEntity item, bool enabled)
        {
            if (IsNullItem(item)) return;
            SetEnabled(actionType, item.Uid, enabled);
        }

        /// <summary>
        /// Revokes the ability to perform the action for this entity. Current state
        /// of the action (toggle / cooldown) is preserved.
        /// </summary>
        public void Revoke(ActionType actionType)
        {
            if (!_actions.TryGetValue(actionType, out var actionState)) return;

            if (!actionState.Enabled) return;

            actionState.Enabled = false;

            // don't store it anymore if its at its initial state.
            if (actionState.IsAtInitialState)
            {
                _actions.Remove(actionType);
            }
            else
            {
                _actions[actionType] = actionState;
            }

            AfterActionChanged();
            Dirty();
        }

        private bool Revoke(ItemActionType actionType, EntityUid item, Dictionary<ItemActionType, ActionState> itemStates,
            ActionState actionState)
        {
            var savedCooldown = actionState.Cooldown;

            if (savedCooldown.HasValue)
            {
                _itemActionCooldowns[(item, actionType)] = savedCooldown.Value;
            }

            var removed = itemStates.Remove(actionType);
            if (itemStates.Count == 0)
            {
                _itemActions.Remove(item);
            }

            return removed;
        }

        /// <summary>
        /// Revokes the knowledge of the item action for the specified item from this entity. Current
        /// cooldown amount will be preserved and restored if this action is later granted.
        ///
        /// This is different from disabling the item - the knowledge of the action is removed entirely,
        /// so the user would no longer see a hotbar action icon for this particular item's action
        /// (though they would still see the icon for the action itself, it would just show
        /// as disabled and not have an item icon).
        /// </summary>
        public void Revoke(ItemActionType actionType, EntityUid item)
        {
            if (!_itemActions.TryGetValue(item, out var itemStates)) return;

            if (!itemStates.TryGetValue(actionType, out var actionState)) return;

            if (!Revoke(actionType, item, itemStates, actionState)) return;
            AfterActionChanged();
            Dirty();
        }

        /// <see cref="Revoke"/>
        public void Revoke(ItemActionType actionType, IEntity item)
        {
            if (IsNullItem(item)) return;
            Revoke(actionType, item.Uid);
        }

        /// <see cref="Revoke"/> applied to all actions currently granted for the item
        public void Revoke(EntityUid item)
        {
            // do it all in a single batch rather than revoking and calling action changed / dirty after each
            var removed = false;
            if (_itemActions.TryGetValue(item, out var itemStates))
            {
                foreach (var stateEntry in itemStates)
                {
                    removed |= Revoke(stateEntry.Key, item, itemStates, stateEntry.Value);
                }
            }

            if (!removed) return;
            AfterActionChanged();
            Dirty();
        }

        /// <see cref="Revoke"/> applied to all actions currently granted for the item
        public void Revoke(IEntity item)
        {
            if (IsNullItem(item)) return;
            Revoke(item.Uid);
        }

        /// <summary>
        /// Toggles the action to the specified value. Works even if the action is on cooldown
        /// or revoked.
        /// </summary>
        public void ToggleAction(ActionType actionType, bool toggleOn)
        {
            Grant(actionType, toggleOn);
        }

        /// <summary>
        /// Toggles the item action to the specified value. No effect if the action has not been granted yet,
        /// is revoked, or the item is not in inventory.
        /// </summary>
        public void ToggleAction(ItemActionType actionType, EntityUid item, bool toggleOn)
        {
            CreateOrUpdate(actionType, item, false, toggleOn: toggleOn);
        }

        /// <see cref="ToggleAction"/>
        public void ToggleAction(ItemActionType actionType, IEntity item, bool toggleOn)
        {
            if (IsNullItem(item)) return;
            ToggleAction(actionType, item.Uid, toggleOn);
        }

        /// <summary>
        /// Clears any cooldowns which have expired beyond the predefined threshold.
        /// As we track cooldowns for items regardless of whether they're granted / revoked,
        /// this should be run periodically to ensure we don't have unbounded growth of
        /// our saved cooldown data.
        /// </summary>
        public void ExpireCooldowns()
        {
            // item actions - only clear saved cooldowns
            var toRemove = new List<(EntityUid item, ItemActionType actionType)>();
            foreach (var itemActionCooldown in _itemActionCooldowns)
            {
                var expiryTime = GameTiming.CurTime - itemActionCooldown.Value.end;
                if (expiryTime > CooldownExpiryThreshold)
                {
                    toRemove.Add(itemActionCooldown.Key);
                }
            }

            foreach (var remove in toRemove)
            {
                _itemActionCooldowns.Remove(remove);
            }

            // actions - only clear cooldowns and remove associated action state
            // if the action is at initial state
            var actionTypesToRemove = new List<ActionType>();
            foreach (var actionState in _actions)
            {
                // ignore it unless we may be able to delete it due to
                // clearing the cooldown
                if (actionState.Value.IsAtInitialStateExceptCooldown)
                {
                    if (!actionState.Value.Cooldown.HasValue)
                    {
                        actionTypesToRemove.Add(actionState.Key);
                        continue;
                    }
                    var expiryTime = GameTiming.CurTime - actionState.Value.Cooldown.Value.Item2;
                    if (expiryTime > CooldownExpiryThreshold)
                    {
                        actionTypesToRemove.Add(actionState.Key);
                    }
                }
            }

            foreach (var remove in actionTypesToRemove)
            {
                _actions.Remove(remove);
            }
        }

        private bool IsNullItem(IEntity item)
        {
            if (item == null)
            {
                Logger.WarningS("action", "tried to modify item action state" +
                                          " for null item");
                return true;
            }

            return false;
        }

        /// <returns>true if the item is in any hand or top-level inventory slot (not inside a container)</returns>
        public bool IsEquipped(EntityUid item)
        {
            if (!EntityManager.TryGetEntity(item, out var itemEntity)) return false;

            return IsEquipped(itemEntity);
        }

        /// <returns>true if the item is in any hand or top-level inventory slot (not inside a container)</returns>
        public bool IsEquipped(IEntity item)
        {
            return _handsComponent.IsHolding(item) || _inventoryComponent.IsEquipped(item);
        }

        /// <summary>
        /// Invoked after a change has been made to an action state in this component.
        /// </summary>
        protected virtual void AfterActionChanged() { }
    }

    [Serializable, NetSerializable]
    public class ActionComponentState : ComponentState
    {
        public Dictionary<ActionType, ActionState> Actions;
        public Dictionary<EntityUid, Dictionary<ItemActionType, ActionState>> ItemActions;

        public ActionComponentState(Dictionary<ActionType, ActionState> actions,
            Dictionary<EntityUid, Dictionary<ItemActionType, ActionState>> itemActions) : base(ContentNetIDs.ACTIONS)
        {
            Actions = actions;
            ItemActions = itemActions;
        }
    }

    [Serializable, NetSerializable]
    public struct ActionState
    {
        /// <summary>
        /// False if this action is not currently allowed to be performed.
        /// </summary>
        public bool Enabled;
        /// <summary>
        /// Only used for toggle actions, indicates whether it's currently toggled on or off
        /// TODO: Eventually this should probably be a byte so we it can toggle through multiple states.
        /// </summary>
        public bool ToggledOn;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
        public bool IsAtInitialState => IsAtInitialStateExceptCooldown && !Cooldown.HasValue;
        public bool IsAtInitialStateExceptCooldown => !Enabled && !ToggledOn;

        /// <summary>
        /// Creates an action state for the indicated type, defaulting to the
        /// initial state.
        /// </summary>
        public ActionState(bool enabled = false, bool toggledOn = false, ValueTuple<TimeSpan, TimeSpan>? cooldown = null)
        {
            Enabled = enabled;
            ToggledOn = toggledOn;
            Cooldown = cooldown;
        }

        public bool IsOnCooldown(TimeSpan curTime)
        {
            if (Cooldown == null) return false;
            return curTime < Cooldown.Value.Item2;
        }
        public bool IsOnCooldown(IGameTiming gameTiming)
        {
            return IsOnCooldown(gameTiming.CurTime);
        }
    }

    [Serializable, NetSerializable]
    public abstract class PerformActionMessage : ComponentMessage
    {
        public readonly ActionType ActionType;

        public PerformActionMessage(ActionType actionType)
        {
            Directed = true;
            ActionType = actionType;
        }
    }

    [Serializable, NetSerializable]
    public abstract class PerformItemActionMessage : ComponentMessage
    {
        public readonly ItemActionType ActionType;
        public readonly EntityUid Item;

        public PerformItemActionMessage(ItemActionType actionType, EntityUid item)
        {
            Directed = true;
            ActionType = actionType;
            Item = item;
        }
    }

    /// <summary>
    /// A message that tells server we want to run the instant action logic.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformInstantActionMessage : PerformActionMessage
    {
        public PerformInstantActionMessage(ActionType actionType) : base(actionType)
        {
        }
    }

    /// <summary>
    /// A message that tells server we want to run the instant action logic.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformInstantItemActionMessage : PerformItemActionMessage
    {
        public PerformInstantItemActionMessage(ItemActionType actionType, EntityUid item) : base(actionType, item)
        {
        }
    }

    /// <summary>
    /// A message that tells server we want to toggle the indicated action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformToggleActionMessage : PerformActionMessage
    {
        /// <summary>
        /// True if we are trying to toggle the action on, false if trying to toggle it off.
        /// </summary>
        public readonly bool ToggleOn;

        public PerformToggleActionMessage(ActionType actionType, bool toggleOn) : base(actionType)
        {
            ToggleOn = toggleOn;
        }
    }

    /// <summary>
    /// A message that tells server we want to toggle the indicated action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformToggleItemActionMessage : PerformItemActionMessage
    {
        /// <summary>
        /// True if we are trying to toggle the action on, false if trying to toggle it off.
        /// </summary>
        public readonly bool ToggleOn;

        public PerformToggleItemActionMessage(ItemActionType actionType, EntityUid item, bool toggleOn) : base(actionType, item)
        {
            ToggleOn = toggleOn;
        }
    }

    /// <summary>
    /// A message that tells server we want to target the provided point with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetPointActionMessage : PerformActionMessage
    {
        /// <summary>
        /// Targeted local coordinates
        /// </summary>
        public readonly EntityCoordinates Target;

        public PerformTargetPointActionMessage(ActionType actionType, EntityCoordinates target) : base(actionType)
        {
            Target = target;
        }
    }

    /// <summary>
    /// A message that tells server we want to target the provided point with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetPointItemActionMessage : PerformItemActionMessage
    {
        /// <summary>
        /// Targeted local coordinates
        /// </summary>
        public readonly EntityCoordinates Target;

        public PerformTargetPointItemActionMessage(ItemActionType actionType, EntityUid item, EntityCoordinates target) : base(actionType, item)
        {
            Target = target;
        }
    }

    /// <summary>
    /// A message that tells server we want to target the provided entity with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetEntityActionMessage : PerformActionMessage
    {
        /// <summary>
        /// Targeted entity
        /// </summary>
        public readonly EntityUid Target;

        public PerformTargetEntityActionMessage(ActionType actionType, EntityUid target) : base(actionType)
        {
            Target = target;
        }
    }

    /// <summary>
    /// A message that tells server we want to target the provided entity with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetEntityItemActionMessage : PerformItemActionMessage
    {
        /// <summary>
        /// Targeted entity
        /// </summary>
        public readonly EntityUid Target;

        public PerformTargetEntityItemActionMessage(ItemActionType actionType, EntityUid item, EntityUid target) : base(actionType, item)
        {
            Target = target;
        }
    }
}
