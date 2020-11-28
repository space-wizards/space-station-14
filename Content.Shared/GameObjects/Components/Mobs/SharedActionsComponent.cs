using System;
using System.Collections.Generic;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
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
        /// Holds all the currently granted and revoked actions and their associated states. If an action
        /// is revoked and is at the initial state (no cooldown, toggled off), it is omitted from this dictionary.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<ActionType, ActionState> _actions = new Dictionary<ActionType, ActionState>();

        public override ComponentState GetComponentState()
        {
            return new ActionComponentState(CreateActionStatesArray());
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is ActionComponentState state))
            {
                return;
            }

            _actions.Clear();
            foreach (var actionState in state.Actions)
            {
                _actions[actionState.ActionType] = actionState;
            }
        }

        /// <summary>
        /// Gets the action state of the action, if it has one.
        ///
        /// Note that this will return false for revoked actions with initial state (no cooldown, toggled off).
        /// </summary>
        /// <param name="actionState">the state, null if this method would return false</param>
        public bool TryGetActionState(ActionType actionType, out ActionState? actionState)
        {
            if (_actions.TryGetValue(actionType, out var actualState))
            {
                actionState = actualState;
                return true;
            }

            actionState = null;
            return false;
        }

        /// <returns>true if an action of the indicated type is currently granted</returns>
        public bool IsGranted(ActionType actionType)
        {
            return _actions.TryGetValue(actionType, out var actionState) && actionState.Granted;
        }

        /// <summary>
        /// Gets the state of all actions. Actions which are revoked and at initial state (no cooldown, toggled off)
        /// are omitted.
        /// </summary>
        protected IEnumerable<ActionState> EnumerateActionStates()
        {
            return _actions.Values;
        }

        /// <summary>
        /// Creates a new array containing all of the current action states.
        /// Actions which are revoked and at initial state (no cooldown, toggled off)
        /// are omitted.
        /// </summary>
        protected ActionState[] CreateActionStatesArray()
        {
            if (_actions.Count == 0) return NoActions;
            var states = new ActionState[_actions.Count];
            // because I don't trust LINQ
            var idx = 0;
            foreach (var actionState in _actions.Values)
            {
                states[idx++] = actionState;
            }

            return states;
        }

        /// <summary>
        /// Grants the entity the ability to perform the action, optionally overriding its
        /// current state with specified values.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be preserved, with specific fields optionally overridden by any of the provided
        /// non-null arguments.
        /// </summary>
        /// <param name="toggleOn">When null, preserves the current toggle status of the action.
        /// When non-null, action will be shown toggled to this value</param>
        /// <param name="cooldown"> When null, preserves the current cooldown status of the action.
        /// When non-null, action cooldown will be set to this value.</param>
        public void GrantAction(ActionType actionType, bool? toggleOn = null,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            var dirty = false;
            if (_actions.TryGetValue(actionType, out var actionState))
            {
                if (!actionState.Granted)
                {
                    dirty = true;
                    actionState.Granted = true;
                }
            }
            else
            {
                dirty = true;
                actionState = new ActionState(actionType, true, toggleOn ?? false);
            }

            if (cooldown.HasValue)
            {
                dirty = true;
                actionState.Cooldown = cooldown;
            }

            if (toggleOn.HasValue)
            {
                actionState.ToggledOn = toggleOn.Value;
            }

            if (!dirty) return;

            _actions[actionType] = actionState;
            AfterGrantAction();
            Dirty();
        }

        /// <summary>
        /// Grants the entity the ability to perform the action, resetting its state
        /// to its initial state and settings its state based on supplied parameters.
        ///
        /// Even if the action was already granted, if the action had any state (cooldown, toggle) prior to this method
        /// being called, it will be reset to initial (no cooldown, toggled off).
        ///
        /// NOTE: Please DO NOT remove this even if it is currently unused, this will
        /// be necessary to implement certain kinds of actions.
        /// </summary>
        /// <param name="toggleOn">action will be shown toggled to this value</param>
        /// <param name="cooldown">action cooldown will be set to this value.</param>
        public void GrantActionFromInitialState(ActionType actionType, bool toggleOn = false,
            (TimeSpan start, TimeSpan end)? cooldown = null)
        {
            _actions.Remove(actionType);
            GrantAction(actionType, toggleOn, cooldown);
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
        public void CooldownAction(ActionType actionType, (TimeSpan start, TimeSpan end)? cooldown)
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
                Logger.DebugS("action", "unrecognized actionType {0}", actionType);
                return;
            }

            if (action.BehaviorType != BehaviorType.Toggle)
            {
                Logger.DebugS("action", "attempted to toggle actionType {0} but it" +
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

    [Serializable, NetSerializable]
    public class ActionComponentState : ComponentState
    {
        public ActionState[] Actions;

        public ActionComponentState(ActionState[] actions) : base(ContentNetIDs.ACTIONS)
        {
            Actions = actions;
        }
    }

    [Serializable, NetSerializable]
    public struct ActionState
    {
        public ActionType ActionType;
        public bool Granted;
        /// <summary>
        /// Only used for toggle actions, indicates whether it's currently toggled on or off
        /// TODO: Eventually this should probably be a byte so we it can toggle through multiple states.
        /// </summary>
        public bool ToggledOn;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;

        /// <summary>
        /// Creates an action state for the indicated type, defaulting to the
        /// initial state (that we omit from our _actions dict).
        /// </summary>
        public ActionState(ActionType actionType, bool granted = false,
            bool toggledOn = false, ValueTuple<TimeSpan, TimeSpan>? cooldown = null)
        {
            ActionType = actionType;
            Granted = granted;
            ToggledOn = toggledOn;
            Cooldown = cooldown;
        }

        public bool IsAtInitialState()
        {
            return !Granted && !ToggledOn && !Cooldown.HasValue;
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
