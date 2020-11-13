using System;
using System.Collections.Generic;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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

        /// <returns>true iff an alert of the indicated id is currently showing</returns>
        public bool IsGranted(ActionType actionType)
        {
            return _actions.ContainsKey(actionType);
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
            foreach (var alertData in _actions.Values)
            {
                states[idx++] = alertData;
            }

            return states;
        }

        protected bool TryGetActionState(ActionType actionType, out ActionState actionState)
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
        /// Invoked after granting an action prior to dirtying the component
        /// </summary>
        protected virtual void AfterGrantAction() { }

        /// <summary>
        /// Invoked after revoking an action prior to dirtying the component
        /// </summary>
        protected virtual void AfterRevokeAction() { }
    }

    [Serializable, NetSerializable]
    public class ActionComponentState : ComponentState
    {
        public ActionState[] Actions;

        public ActionComponentState(ActionState[] actions) : base(ContentNetIDs.ALERTS)
        {
            Actions = actions;
        }
    }

    [Serializable, NetSerializable]
    public struct ActionState
    {
        public ActionType ActionType;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
    }
}
