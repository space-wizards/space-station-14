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
    /// Actions can be granted and revoked, and have an associated cooldown and toggle status (only
    /// used for toggle-able actions). Even revoked actions can have state (so you can, for example,
    /// see whether you've toggled your flashlight on even when you're stunned and cannot actually
    /// change it).
    ///
    /// In addition to granting an action directly to the owner entity,
    /// actions can be "bound" to an item if they are configured with itemBound: true,
    /// such that the action is only usable when the item is in a hand or equip slot. A given
    /// action type can be bound to multiple items in inventory.
    ///
    /// Note that item-bound action cooldowns are currently only tied to the player, not the item
    /// (but they are still preserved if the item is dropped and picked up). Eventually
    /// we may want some item-bound actions to live on the item itself.
    /// </summary>
    public abstract class SharedActionsComponent : Component
    {
        private static readonly ActionState[] NoActions = new ActionState[0];

        [Dependency]
        protected readonly ActionManager ActionManager = default!;
        [Dependency]
        protected readonly IGameTiming GameTiming = default!;

        public override string Name => "ActionsUI";
        public override uint? NetID => ContentNetIDs.ACTIONS;

        /// <summary>
        /// Holds all the currently granted and revoked actions and their associated states.
        /// We maintain the following invariants:
        ///
        /// 1. _ownerBound only contains actiontypes whose corresponding ActionPrototype is not ItemBound,
        /// and _itemBoundActions only contains actiontypes whose corresponding ActionPrototype is ItemBound.
        /// 2. Owner bound actions are removed if they are at the initial state (revoked, no cooldown, toggled off).
        /// 3. Item bound actions are removed when they leave inventory when they have no cooldown or after their cooldown has
        /// expired for a long enough time. This ensures a player can't clear their cooldowns by dropping and picking up an item,
        /// while also allowing players to still see their item-bound actions for equipped items even when they are
        /// temporarily revoked by some effect.
        /// 4. There are no empty dictionaries in itemBoundActions.
        /// </summary>
        [ViewVariables]
        private Dictionary<ActionType, ActionState> _ownerBoundActions = new Dictionary<ActionType, ActionState>();
        private Dictionary<ActionType, Dictionary<EntityUid, ActionState>> _itemBoundActions = new Dictionary<ActionType, Dictionary<EntityUid, ActionState>>();

        public override ComponentState GetComponentState()
        {
            return new ActionComponentState(_ownerBoundActions, _itemBoundActions);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is ActionComponentState state))
            {
                return;
            }
            _ownerBoundActions = state.OwnerBound;
            _itemBoundActions = state.ItemBound;
        }

        /// <summary>
        /// Gets the action states associated with the specified action type, if it has any.
        /// A given action type can have one owner bound action state or multiple item bound action states,
        /// but not both.
        /// </summary>
        /// <param name="ownerBoundState">owner bound action state when BindingType is returned as OwnerBound</param>
        /// <param name="itemBoundState">map from item ID to action state when BindingType is returned as ItemBound</param>
        /// <returns>the type of bindings that were retrieved, Nonexistent if there are no
        /// bindings at all for this action type.</returns>
        public BindingType TryGetActionStates(ActionType actionType, out ActionState ownerBoundState,
            out IReadOnlyDictionary<EntityUid, ActionState> itemBoundState)
        {
            if (_ownerBoundActions.TryGetValue(actionType, out ownerBoundState))
            {
                itemBoundState = null;
                return BindingType.OwnerBound;
            }
            if (_itemBoundActions.TryGetValue(actionType, out var rawItemBoundState))
            {
                itemBoundState = rawItemBoundState;
                return BindingType.ItemBound;
            }

            itemBoundState = null;
            return BindingType.Nonexistent;
        }

        /// <summary>
        /// Gets all action types that are bound to either the owner or an item.
        /// </summary>
        protected IEnumerable<ActionType> EnumerateBoundActions()
        {
            return _ownerBoundActions.Keys.Concat(_itemBoundActions.Keys);
        }

        /// <summary>
        /// Grants the entity the ability to perform the action, bound to this entity, optionally overriding its
        /// current state with specified values. This will fail if the action prototype for this ActionType
        /// is ItemBound.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be preserved, with specific fields optionally overridden by any of the provided
        /// non-null arguments.
        /// </summary>
        /// <param name="toggleOn">When null, preserves the current toggle status of the action.
        /// When non-null, action will be shown toggled to this value</param>
        /// <param name="cooldown"> When null, preserves the current cooldown status of the action.
        /// When non-null, action cooldown will be set to this value.</param>
        public void GrantOwnerBoundAction(ActionType actionType, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            if (!ActionManager.TryGet(actionType, out var action))
            {
                Logger.WarningS("action", "unknown actionType {0}", actionType);
                return;
            }

            if (action.ItemBound)
            {
                Logger.WarningS("action", "trying to grant action {0} bound to owner, but this action is " +
                                          "item bound. The action must have itemBound: false in order to be granted bound to owner", actionType);
                return;
            }

            var dirty = false;
            if (_ownerBoundActions.TryGetValue(actionType, out var actionState))
            {
                // this method is for granting the action, so ensure it's now granted,
                // preserving existing state
                if (!actionState.Granted)
                {
                    dirty = true;
                    actionState.Granted = true;
                }
            }
            else
            {
                // no bindings at all for this action, create it anew
                dirty = true;
                actionState = new ActionState(true, toggleOn ?? false);
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

            _ownerBoundActions[actionType] = actionState;
            AfterGrantAction();
            Dirty();
        }

        /// <summary>
        /// Grants the entity the ability to perform the action bound to a specific item, optionally overriding its
        /// current state with specified values. This will fail if the action prototype for this ActionType
        /// is OwnerBound.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be preserved, with specific fields optionally overridden by any of the provided
        /// non-null arguments.
        /// </summary>
        /// <param name="toggleOn">When null, preserves the current toggle status of the action.
        /// When non-null, action will be shown toggled to this value</param>
        /// <param name="cooldown"> When null, preserves the current cooldown status of the action.
        /// When non-null, action cooldown will be set to this value.</param>
        public void GrantBoundAction(ActionType actionType, IEntity boundItem, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            if (boundItem == null)
            {
                Logger.WarningS("action", "cannot bind action {0} to null item", actionType);
                return;
            }

            if (!ActionManager.TryGet(actionType, out var action))
            {
                Logger.WarningS("action", "unknown actionType {0}", actionType);
                return;
            }

            if (action.OwnerBound)
            {
                Logger.WarningS("action", "trying to grant action {0} as item bound, but this action is " +
                                          "owner bound. The action must have itemBound: true to be able to grant it bound to an item.", actionType);
                return;
            }
            var dirty = false;
            // this will be overwritten if we find the value in our dict, otherwise
            // we will use this as our new action state.
            var actionState = new ActionState(true, toggleOn ?? false);
            if (_itemBoundActions.TryGetValue(actionType, out var itemBindings))
            {
                // we already have some bindings for this action
                // look up the binding of this action to this item, creating it if we don't have one
                if (!itemBindings.TryGetValue(boundItem.Uid, out actionState))
                {
                    // didn't exist for this item, we will create the binding
                    dirty = true;
                }

                // this method is for granting the action, so ensure it's now granted
                if (!actionState.Granted)
                {
                    dirty = true;
                    actionState.Granted = true;
                }
            }
            else
            {
                // no bindings at all for this action, create it anew
                dirty = true;
                itemBindings = new Dictionary<EntityUid, ActionState>();
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

            itemBindings[boundItem.Uid] = actionState;
            _itemBoundActions[actionType] = itemBindings;
            AfterGrantAction();
            Dirty();
        }

        /// <summary>
        /// Grants the entity the ability to perform the action, bound to this entity, resetting its state
        /// to its initial state and settings its state based on supplied parameters. This will fail if the
        /// action prototype for this ActionType is ItemBound.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be reset to initial (no cooldown, toggled off).
        /// </summary>
        /// <param name="toggleOn">action will be shown toggled to this value</param>
        /// <param name="cooldown">action cooldown will be set to this value (by default the cooldown is cleared).</param>
        public void GrantOwnerBoundActionFromInitialState(ActionType actionType, bool toggleOn = false,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            _ownerBoundActions.Remove(actionType);
            GrantOwnerBoundAction(actionType, toggleOn, cooldown);
        }

        /// <summary>
        /// Grants the entity the ability to perform the action bound to an item, resetting its state
        /// to its initial state and settings its state based on supplied parameters. This will fail if the
        /// action prototype for this ActionType is not ItemBound.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be reset to initial (no cooldown, toggled off).
        /// </summary>
        /// <param name="toggleOn">action will be shown toggled to this value</param>
        /// <param name="cooldown">action cooldown will be set to this value (by default the cooldown is cleared).</param>
        public void GrantBoundActionFromInitialState(ActionType actionType, IEntity boundItem, bool toggleOn = false,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            if (_itemBoundActions.TryGetValue(actionType, out var actionBindings))
            {
                actionBindings.Remove(boundItem.Uid);
                if (actionBindings.Count == 0)
                {
                    _itemBoundActions.Remove(actionType);
                }
            }
            GrantBoundAction(actionType, boundItem, toggleOn, cooldown);
        }

        /// <summary>
        /// Sets the cooldown for the owner bound action. Actions on cooldown cannot be used.
        ///
        /// This will work even if the action is revoked -
        /// for example if there's an ability with a cooldown which is temporarily unusable due
        /// to the player being stunned, the cooldown will still tick down even while the player
        /// is stunned.
        ///
        /// Setting cooldown to null clears it.
        /// </summary>
        public void CooldownOwnerBoundAction(ActionType actionType, (TimeSpan start, TimeSpan end)? cooldown)
        {
            if (_actions.TryGetValue(actionType, out var actionState))
            {
                actionState.Cooldown = cooldown;
            }
            else
            {
                // the action was revoked and in initial state so it wasn't in the dict,
                // we need to create it and add it to our dict if
                // the cooldown has a value
                if (!cooldown.HasValue) return;

                actionState = new ActionState(actionType);
                actionState.Cooldown = cooldown;
            }

            _actions[actionType] = actionState;
            AfterCooldownAction();
            Dirty();
        }

        /// <summary>
        /// Revokes the ability to perform the action for this entity. Current state
        /// of the action (toggle / cooldown) is preserved.
        /// </summary>
        public void RevokeAction(ActionType actionType)
        {
            if (!_actions.TryGetValue(actionType, out var actionState)) return;

            if (!actionState.Granted) return;

            actionState.Granted = false;

            // don't store it anymore if its at its initial state.
            if (actionState.IsAtInitialState())
            {
                _actions.Remove(actionType);
            }
            else
            {
                _actions[actionType] = actionState;
            }

            AfterRevokeAction();
            Dirty();
        }

        /// <summary>
        /// Toggles the action to the specified value. Works even if the action is on cooldown
        /// or revoked.
        /// </summary>
        public void ToggleAction(ActionType actionType, bool toggleOn)
        {
            if (!ActionManager.TryGet(actionType, out var action))
            {
                Logger.WarningS("action", "unrecognized actionType {0}", actionType);
                return;
            }

            if (action.BehaviorType != BehaviorType.Toggle)
            {
                Logger.WarningS("action", "attempted to toggle actionType {0} but it" +
                                          " is not a Toggle action.", actionType);
                return;
            }

            if (_actions.TryGetValue(actionType, out var actionState))
            {
                actionState.ToggledOn = toggleOn;
            }
            else
            {
                // it was revoked at initial state, thus not stored
                // in our dict. Create it and add it to the dict
                // if it's going to be set to non-initial state
                if (!toggleOn) return;

                actionState = new ActionState(actionType, false, toggleOn);
            }

            if (actionState.IsAtInitialState())
            {
                _actions.Remove(actionType);
            }

            _actions[actionType] = actionState;
            AfterToggleAction();
            Dirty();
        }

        /// <summary>
        /// Invoked after granting an action prior to dirtying the component
        /// </summary>
        protected virtual void AfterGrantAction() { }

        /// <summary>
        /// Invoked after setting an action cooldown prior to dirtying the component
        /// </summary>
        protected virtual void AfterCooldownAction() { }

        /// <summary>
        /// Invoked after revoking an action prior to dirtying the component
        /// </summary>
        protected virtual void AfterRevokeAction() { }

        /// <summary>
        /// Invoked after toggling a toggle action prior to dirtying the component
        /// </summary>
        protected virtual void AfterToggleAction() { }
    }

    public enum BindingType
    {
        Nonexistent,
        OwnerBound,
        ItemBound
    }

    [Serializable, NetSerializable]
    public class ActionComponentState : ComponentState
    {
        public Dictionary<ActionType, ActionState> OwnerBound;
        public Dictionary<ActionType, Dictionary<EntityUid, ActionState>> ItemBound;

        public ActionComponentState(Dictionary<ActionType, ActionState> ownerBound,
            Dictionary<ActionType, Dictionary<EntityUid, ActionState>> itemBound) : base(ContentNetIDs.ACTIONS)
        {
            OwnerBound = ownerBound;
            ItemBound = itemBound;
        }
    }

    [Serializable, NetSerializable]
    public struct ActionState
    {
        public bool Granted;
        /// <summary>
        /// Only used for toggle actions, indicates whether it's currently toggled on or off
        /// TODO: Eventually this should probably be a byte so we it can toggle through multiple states.
        /// </summary>
        public bool ToggledOn;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
        public bool IsAtInitialState => !Granted && !ToggledOn && !Cooldown.HasValue;

        /// <summary>
        /// Creates an action state for the indicated type, defaulting to the
        /// initial state.
        /// </summary>
        public ActionState(bool granted = false, bool toggledOn = false, ValueTuple<TimeSpan, TimeSpan>? cooldown = null)
        {
            Granted = granted;
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
}
