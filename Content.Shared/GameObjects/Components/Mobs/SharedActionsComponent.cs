using System;
using System.Collections.Generic;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
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
    /// </summary>

    public abstract class SharedActionsComponent : Component
    {
        private static readonly ActionState[] NoActions = new ActionState[0];

        [Dependency]
        protected readonly ActionManager ActionManager = default!;

        public override string Name => "ActionsUI";
        public override uint? NetID => ContentNetIDs.ACTIONS;

        /// <summary>
        /// Holds all the currently granted actions.
        /// </summary>
        [ViewVariables]
        private Dictionary<ActionType, ActionState> _actions = new Dictionary<ActionType, ActionState>();

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

        /// <returns>true iff an action of the indicated id is currently showing</returns>
        public bool IsGranted(ActionType actionType)
        {
            return _actions.ContainsKey(actionType);
        }

        /// <param name="toggledOn">current toggled status</param>
        /// <returns>true iff the action is granted and a Toggle action</returns>
        public bool IsToggleable(ActionType actionType, out bool toggledOn)
        {
            if (_actions.TryGetValue(actionType, out var actionState))
            {
                if (ActionManager.TryGet(actionType, out var action) && action.BehaviorType == BehaviorType.Toggle)
                {
                    toggledOn = actionState.ToggledOn;
                    return true;
                }
            }

            toggledOn = false;
            return false;
        }

        /// <summary>
        /// Gets the state of all currently granted actions
        /// </summary>
        protected IEnumerable<ActionState> EnumerateActionStates()
        {
            return _actions.Values;
        }

        /// <summary>
        /// Creates a new array containing all of the current action states.
        /// </summary>
        /// <returns></returns>
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
        /// Gets the current state of the action, if granted. Returns false if action is
        /// not granted.
        /// </summary>
        protected bool TryGetGrantedActionState(ActionType actionType, out ActionState actionState)
        {
            return _actions.TryGetValue(actionType, out actionState);
        }

        /// <summary>
        /// Grants the entity the ability to perform the action. Does nothing
        /// if entity already has been granted this action.
        /// </summary>
        public void GrantAction(ActionType actionType)
        {
            if (_actions.ContainsKey(actionType)) return;

            _actions[actionType] = new ActionState()
                {ActionType = actionType};

            AfterGrantAction();

            Dirty();
        }

        /// <summary>
        /// Revokes the ability to perform the action for this entity. No effect
        /// if action is not granted.
        /// </summary>
        public void RevokeAction(ActionType actionType)
        {

            if (_actions.Remove(actionType))
            {
                AfterRevokeAction();
                Dirty();
            }
        }

        /// <summary>
        /// Toggles the action to the specified value. Only has an effect if the action
        /// is granted and is a Toggle action.
        /// </summary>
        protected void ToggleAction(ActionType actionType, bool toggleOn)
        {
            if (!_actions.TryGetValue(actionType, out var curState))
            {
                Logger.DebugS("action", "attempted to toggle actionType {0} which is not" +
                                        " currently granted", actionType);
                return;
            }

            if (curState.ToggledOn == toggleOn)
            {
                Logger.DebugS("action", "attempted to toggle actionType {0} to {1}" +
                                        " but it is already {1}", actionType, toggleOn ? "on" : "off");
                return;
            }

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


            curState.ToggledOn = toggleOn;
            _actions[actionType] = curState;
            AfterToggleAction();
            Dirty();

        }

        /// <summary>
        /// Invoked after granting an action prior to dirtying the component
        /// </summary>
        protected virtual void AfterGrantAction() { }

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
        /// <summary>
        /// Only used for toggle actions, indicates whether it's currently toggled on or off
        /// </summary>
        public bool ToggledOn;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
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
