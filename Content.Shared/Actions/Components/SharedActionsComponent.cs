using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Actions.Components
{
    /// <summary>
    /// Manages the actions available to an entity.
    /// Should only be used for player-controlled entities.
    ///
    /// Actions are granted directly to the owner entity. Item actions are granted via a particular item which
    /// must be in the owner's inventory (the action is revoked when item leaves the owner's inventory). This
    /// should almost always be done via ItemActionsComponent on the item entity (which also tracks the
    /// cooldowns associated with the actions on that item).
    ///
    /// Actions can still have an associated state even when revoked. For example, a flashlight toggle action
    /// may be unusable while the player is stunned, but this component will still have an entry for the action
    /// so the user can see whether it's currently toggled on or off.
    /// </summary>
    [NetworkedComponent()]
    public abstract class SharedActionsComponent : Component
    {
        private static readonly TimeSpan CooldownExpiryThreshold = TimeSpan.FromSeconds(10);

        [Dependency]
        protected readonly ActionManager ActionManager = default!;
        [Dependency]
        protected readonly IGameTiming GameTiming = default!;
        [Dependency]
        protected readonly IEntityManager EntityManager = default!;

        /// <summary>
        /// Actions granted to this entity as soon as they spawn, regardless
        /// of the status of the entity.
        /// </summary>
        public IEnumerable<ActionType> InnateActions => _innateActions ?? Enumerable.Empty<ActionType>();
        [DataField("innateActions")]
        private List<ActionType>? _innateActions = null;


        // entries are removed from this if they are at the initial state (not enabled, no cooldown, toggled off).
        // a system runs which periodically removes cooldowns from entries when they are revoked and their
        // cooldowns have expired for a long enough time, also removing the entry if it is then at initial state.
        // This helps to keep our component state smaller.
        [ViewVariables]
        private Dictionary<ActionType, ActionState> _actions = new();

        // Holds item action states. Item actions are only added to this when granted, and are removed
        // when revoked or when they leave inventory. This is almost entirely handled by ItemActionsComponent on
        // item entities.
        [ViewVariables]
        private Dictionary<EntityUid, Dictionary<ItemActionType, ActionState>> _itemActions =
            new();

        protected override void Startup()
        {
            base.Startup();
            foreach (var actionType in InnateActions)
            {
                Grant(actionType);
            }
        }


        public override ComponentState GetComponentState()
        {
            return new ActionComponentState(_actions, _itemActions);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not ActionComponentState state)
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
        public bool TryGetItemActionStates(EntityUid item, [NotNullWhen((true))] out IReadOnlyDictionary<ItemActionType, ActionState>? itemActionStates)
        {
            if (_itemActions.TryGetValue(item, out var actualItemActionStates))
            {
                itemActionStates = actualItemActionStates;
                return true;
            }

            itemActionStates = null;
            return false;
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

        /// <returns>true if the action is granted and enabled (if item action, if granted and enabled for any item)</returns>
        public bool IsGranted(BaseActionPrototype actionType)
        {
            return actionType switch
            {
                ActionPrototype actionPrototype => IsGranted(actionPrototype.ActionType),
                ItemActionPrototype itemActionPrototype => IsGranted(itemActionPrototype.ActionType),
                _ => false
            };
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

        /// <summary>
        /// Gets all action types that have non-initial state (granted, have a cooldown, or toggled on).
        /// </summary>
        public IReadOnlyDictionary<ActionType, ActionState> ActionStates()
        {
            return _actions;
        }

        /// <summary>
        /// Gets all items that have actions currently granted (that are not revoked
        /// and still in inventory).
        /// Map from item uid -> (action type -> associated action state)
        /// PLEASE DO NOT MODIFY THE INNER DICTIONARY! I CANNOT CAST IT TO IReadOnlyDictionary!
        /// </summary>
        public IReadOnlyDictionary<EntityUid,Dictionary<ItemActionType, ActionState>> ItemActionStates()
        {
            return _itemActions;
        }

        /// <summary>
        /// Creates or updates the action state with the supplied non-null values
        /// </summary>
        private void GrantOrUpdate(ActionType actionType, bool? enabled = null, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null, bool clearCooldown = false)
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
            AfterActionChanged();
            Dirty();
        }

        /// <summary>
        /// Intended to only be used by ItemActionsComponent.
        /// Updates the state of the item action provided by the item, granting the action
        /// if it is not yet granted to the player. Should be called whenever the
        /// status changes. The existing state will be completely overwritten by the new state.
        /// </summary>
        public void GrantOrUpdateItemAction(ItemActionType actionType, EntityUid item, ActionState state)
        {
            if (!_itemActions.TryGetValue(item, out var itemStates))
            {
                itemStates = new Dictionary<ItemActionType, ActionState>();
                _itemActions[item] = itemStates;
            }

            itemStates[actionType] = state;
            AfterActionChanged();
            Dirty();
        }

        /// <summary>
        /// Intended to only be used by ItemActionsComponent. Revokes the item action so the player no longer
        /// sees it and can no longer use it.
        /// </summary>
        public void RevokeItemAction(ItemActionType actionType, EntityUid item)
        {
            if (!_itemActions.TryGetValue(item, out var itemStates))
                return;

            itemStates.Remove(actionType);
            if (itemStates.Count == 0)
            {
                _itemActions.Remove(item);
            }
            AfterActionChanged();
            Dirty();
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
           GrantOrUpdate(actionType, true, toggleOn, cooldown);
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
            GrantOrUpdate(actionType, cooldown: cooldown, clearCooldown: true);
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

        /// <summary>
        /// Toggles the action to the specified value. Works even if the action is on cooldown
        /// or revoked.
        /// </summary>
        public void ToggleAction(ActionType actionType, bool toggleOn)
        {
            Grant(actionType, toggleOn);
        }

        /// <summary>
        /// Clears any cooldowns which have expired beyond the predefined threshold.
        /// this should be run periodically to ensure we don't have unbounded growth of
        /// our saved action data, and keep our component state sent to the client as minimal as possible.
        /// </summary>
        public void ExpireCooldowns()
        {

            // actions - only clear cooldowns and remove associated action state
            // if the action is at initial state
            var actionTypesToRemove = new List<ActionType>();
            foreach (var (actionType, actionState) in _actions)
            {
                // ignore it unless we may be able to delete it due to
                // clearing the cooldown
                if (!actionState.IsAtInitialStateExceptCooldown) continue;
                if (!actionState.Cooldown.HasValue)
                {
                    actionTypesToRemove.Add(actionType);
                    continue;
                }
                var expiryTime = GameTiming.CurTime - actionState.Cooldown.Value.Item2;
                if (expiryTime > CooldownExpiryThreshold)
                {
                    actionTypesToRemove.Add(actionType);
                }
            }

            foreach (var remove in actionTypesToRemove)
            {
                _actions.Remove(remove);
            }
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
            Dictionary<EntityUid, Dictionary<ItemActionType, ActionState>> itemActions)
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
        public (TimeSpan start, TimeSpan end)? Cooldown;
        public bool IsAtInitialState => IsAtInitialStateExceptCooldown && !Cooldown.HasValue;
        public bool IsAtInitialStateExceptCooldown => !Enabled && !ToggledOn;

        /// <summary>
        /// Creates an action state for the indicated type, defaulting to the
        /// initial state.
        /// </summary>
        public ActionState(bool enabled = false, bool toggledOn = false, (TimeSpan start, TimeSpan end)? cooldown = null)
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

        public bool Equals(ActionState other)
        {
            return Enabled == other.Enabled && ToggledOn == other.ToggledOn && Nullable.Equals(Cooldown, other.Cooldown);
        }

        public override bool Equals(object? obj)
        {
            return obj is ActionState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Enabled, ToggledOn, Cooldown);
        }
    }

    [Serializable, NetSerializable]
#pragma warning disable 618
    public abstract class BasePerformActionMessage : ComponentMessage
#pragma warning restore 618
    {
        public abstract BehaviorType BehaviorType { get; }
    }

    [Serializable, NetSerializable]
    public abstract class PerformActionMessage : BasePerformActionMessage
    {
        public readonly ActionType ActionType;

        protected PerformActionMessage(ActionType actionType)
        {
            Directed = true;
            ActionType = actionType;
        }
    }

    [Serializable, NetSerializable]
    public abstract class PerformItemActionMessage : BasePerformActionMessage
    {
        public readonly ItemActionType ActionType;
        public readonly EntityUid Item;

        protected PerformItemActionMessage(ItemActionType actionType, EntityUid item)
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
        public override BehaviorType BehaviorType => BehaviorType.Instant;

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
        public override BehaviorType BehaviorType => BehaviorType.Instant;

        public PerformInstantItemActionMessage(ItemActionType actionType, EntityUid item) : base(actionType, item)
        {
        }
    }

    public interface IToggleActionMessage
    {
        bool ToggleOn { get; }
    }

    public interface ITargetPointActionMessage
    {
        /// <summary>
        /// Targeted local coordinates
        /// </summary>
        EntityCoordinates Target { get; }
    }

    public interface ITargetEntityActionMessage
    {
        /// <summary>
        /// Targeted entity
        /// </summary>
        EntityUid Target { get; }
    }

    /// <summary>
    /// A message that tells server we want to toggle on the indicated action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformToggleOnActionMessage : PerformActionMessage, IToggleActionMessage
    {
        public override BehaviorType BehaviorType => BehaviorType.Toggle;
        public bool ToggleOn => true;
        public PerformToggleOnActionMessage(ActionType actionType) : base(actionType) { }
    }

    /// <summary>
    /// A message that tells server we want to toggle off the indicated action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformToggleOffActionMessage : PerformActionMessage, IToggleActionMessage
    {
        public override BehaviorType BehaviorType => BehaviorType.Toggle;
        public bool ToggleOn => false;
        public PerformToggleOffActionMessage(ActionType actionType) : base(actionType) { }
    }

    /// <summary>
    /// A message that tells server we want to toggle on the indicated action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformToggleOnItemActionMessage : PerformItemActionMessage, IToggleActionMessage
    {
        public override BehaviorType BehaviorType => BehaviorType.Toggle;
        public bool ToggleOn => true;
        public PerformToggleOnItemActionMessage(ItemActionType actionType, EntityUid item) : base(actionType, item) { }
    }

    /// <summary>
    /// A message that tells server we want to toggle off the indicated action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformToggleOffItemActionMessage : PerformItemActionMessage, IToggleActionMessage
    {
        public override BehaviorType BehaviorType => BehaviorType.Toggle;
        public bool ToggleOn => false;
        public PerformToggleOffItemActionMessage(ItemActionType actionType, EntityUid item) : base(actionType, item) { }
    }

    /// <summary>
    /// A message that tells server we want to target the provided point with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetPointActionMessage : PerformActionMessage, ITargetPointActionMessage
    {
        public override BehaviorType BehaviorType => BehaviorType.TargetPoint;
        private readonly EntityCoordinates _target;
        public EntityCoordinates Target => _target;

        public PerformTargetPointActionMessage(ActionType actionType, EntityCoordinates target) : base(actionType)
        {
            _target = target;
        }
    }

    /// <summary>
    /// A message that tells server we want to target the provided point with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetPointItemActionMessage : PerformItemActionMessage, ITargetPointActionMessage
    {
        private readonly EntityCoordinates _target;
        public EntityCoordinates Target => _target;
        public override BehaviorType BehaviorType => BehaviorType.TargetPoint;

        public PerformTargetPointItemActionMessage(ItemActionType actionType, EntityUid item, EntityCoordinates target) : base(actionType, item)
        {
            _target = target;
        }
    }

    /// <summary>
    /// A message that tells server we want to target the provided entity with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetEntityActionMessage : PerformActionMessage, ITargetEntityActionMessage
    {
        public override BehaviorType BehaviorType => BehaviorType.TargetEntity;
        private readonly EntityUid _target;
        public EntityUid Target => _target;

        public PerformTargetEntityActionMessage(ActionType actionType, EntityUid target) : base(actionType)
        {
            _target = target;
        }
    }

    /// <summary>
    /// A message that tells server we want to target the provided entity with a particular action.
    /// </summary>
    [Serializable, NetSerializable]
    public class PerformTargetEntityItemActionMessage : PerformItemActionMessage, ITargetEntityActionMessage
    {
        public override BehaviorType BehaviorType => BehaviorType.TargetEntity;
        private readonly EntityUid _target;
        public EntityUid Target => _target;

        public PerformTargetEntityItemActionMessage(ItemActionType actionType, EntityUid item, EntityUid target) : base(actionType, item)
        {
            _target = target;
        }
    }
}
