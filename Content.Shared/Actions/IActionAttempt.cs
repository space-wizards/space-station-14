using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Content.Shared.Actions
{
    /// <summary>
    /// An attempt to perform a specific action. Main purpose of this interface is to
    /// reduce code duplication related to handling attempts to perform non-item vs item actions by
    /// providing a single interface for various functionality that needs to be performed on both.
    /// </summary>
    public interface IActionAttempt
    {
        /// <summary>
        /// Action Prototype attempting to be performed
        /// </summary>
        BaseActionPrototype Action { get; }
#pragma warning disable 618
        ComponentMessage PerformInstantActionMessage();
        ComponentMessage PerformToggleActionMessage(bool on);
        ComponentMessage PerformTargetPointActionMessage(PointerInputCmdHandler.PointerInputCmdArgs args);
        ComponentMessage PerformTargetEntityActionMessage(PointerInputCmdHandler.PointerInputCmdArgs args);
#pragma warning restore 618
        /// <summary>
        /// Tries to get the action state for this action from the actionsComponent, returning
        /// true if found.
        /// </summary>
        bool TryGetActionState(SharedActionsComponent actionsComponent, out ActionState actionState);

        /// <summary>
        /// Toggles the action within the provided action component
        /// </summary>
        void ToggleAction(SharedActionsComponent actionsComponent, bool toggleOn);

        /// <summary>
        /// Perform the server-side logic of the action
        /// </summary>
        void DoInstantAction(IEntity player);

        /// <summary>
        /// Perform the server-side logic of the toggle action
        /// </summary>
        /// <returns>true if the attempt to toggle was successful, meaning the state should be toggled to the
        /// indicated value</returns>
        bool DoToggleAction(IEntity player, bool on);

        /// <summary>
        /// Perform the server-side logic of the target point action
        /// </summary>
        void DoTargetPointAction(IEntity player, EntityCoordinates target);

        /// <summary>
        /// Perform the server-side logic of the target entity action
        /// </summary>
        void DoTargetEntityAction(IEntity player, IEntity target);
    }

    public class ActionAttempt : IActionAttempt
    {
        private readonly ActionPrototype _action;

        public BaseActionPrototype Action => _action;

        public ActionAttempt(ActionPrototype action)
        {
            _action = action;
        }

        public bool TryGetActionState(SharedActionsComponent actionsComponent, out ActionState actionState)
        {
            return actionsComponent.TryGetActionState(_action.ActionType, out actionState);
        }

        public void ToggleAction(SharedActionsComponent actionsComponent, bool toggleOn)
        {
            actionsComponent.ToggleAction(_action.ActionType, toggleOn);
        }

        public void DoInstantAction(IEntity player)
        {
            _action.InstantAction.DoInstantAction(new InstantActionEventArgs(player, _action.ActionType));
        }

        public bool DoToggleAction(IEntity player, bool on)
        {
            return _action.ToggleAction.DoToggleAction(new ToggleActionEventArgs(player, _action.ActionType, on));
        }

        public void DoTargetPointAction(IEntity player, EntityCoordinates target)
        {
            _action.TargetPointAction.DoTargetPointAction(new TargetPointActionEventArgs(player, target, _action.ActionType));
        }

        public void DoTargetEntityAction(IEntity player, IEntity target)
        {
            _action.TargetEntityAction.DoTargetEntityAction(new TargetEntityActionEventArgs(player, _action.ActionType,
                target));
        }

#pragma warning disable 618
        public ComponentMessage PerformInstantActionMessage()
#pragma warning restore 618
        {
            return new PerformInstantActionMessage(_action.ActionType);
        }

#pragma warning disable 618
        public ComponentMessage PerformToggleActionMessage(bool toggleOn)
#pragma warning restore 618
        {
            if (toggleOn)
            {
                return new PerformToggleOnActionMessage(_action.ActionType);
            }
            return new PerformToggleOffActionMessage(_action.ActionType);
        }

#pragma warning disable 618
        public ComponentMessage PerformTargetPointActionMessage(PointerInputCmdHandler.PointerInputCmdArgs args)
#pragma warning restore 618
        {
            return new PerformTargetPointActionMessage(_action.ActionType, args.Coordinates);
        }

#pragma warning disable 618
        public ComponentMessage PerformTargetEntityActionMessage(PointerInputCmdHandler.PointerInputCmdArgs args)
#pragma warning restore 618
        {
            return new PerformTargetEntityActionMessage(_action.ActionType, args.EntityUid);
        }

        public override string ToString()
        {
            return $"{nameof(_action)}: {_action.ActionType}";
        }
    }

    public class ItemActionAttempt : IActionAttempt
    {
        private readonly ItemActionPrototype _action;
        private readonly IEntity _item;
        private readonly ItemActionsComponent _itemActions;

        public BaseActionPrototype Action => _action;

        public ItemActionAttempt(ItemActionPrototype action, IEntity item, ItemActionsComponent itemActions)
        {
            _action = action;
            _item = item;
            _itemActions = itemActions;
        }

        public void DoInstantAction(IEntity player)
        {
            _action.InstantAction.DoInstantAction(new InstantItemActionEventArgs(player, _item, _action.ActionType));
        }

        public bool DoToggleAction(IEntity player, bool on)
        {
            return _action.ToggleAction.DoToggleAction(new ToggleItemActionEventArgs(player, on, _item, _action.ActionType));
        }

        public void DoTargetPointAction(IEntity player, EntityCoordinates target)
        {
            _action.TargetPointAction.DoTargetPointAction(new TargetPointItemActionEventArgs(player, target, _item,
                _action.ActionType));
        }

        public void DoTargetEntityAction(IEntity player, IEntity target)
        {
            _action.TargetEntityAction.DoTargetEntityAction(new TargetEntityItemActionEventArgs(player, target,
                _item, _action.ActionType));
        }

        public bool TryGetActionState(SharedActionsComponent actionsComponent, out ActionState actionState)
        {
            return actionsComponent.TryGetItemActionState(_action.ActionType, _item, out actionState);
        }

        public void ToggleAction(SharedActionsComponent actionsComponent, bool toggleOn)
        {
            _itemActions.Toggle(_action.ActionType, toggleOn);
        }

#pragma warning disable 618
        public ComponentMessage PerformInstantActionMessage()
#pragma warning restore 618
        {
            return new PerformInstantItemActionMessage(_action.ActionType, _item.Uid);
        }

#pragma warning disable 618
        public ComponentMessage PerformToggleActionMessage(bool toggleOn)
#pragma warning restore 618
        {
            if (toggleOn)
            {
                return new PerformToggleOnItemActionMessage(_action.ActionType, _item.Uid);
            }
            return new PerformToggleOffItemActionMessage(_action.ActionType, _item.Uid);
        }

#pragma warning disable 618
        public ComponentMessage PerformTargetPointActionMessage(PointerInputCmdHandler.PointerInputCmdArgs args)
#pragma warning restore 618
        {
            return new PerformTargetPointItemActionMessage(_action.ActionType, _item.Uid, args.Coordinates);
        }

#pragma warning disable 618
        public ComponentMessage PerformTargetEntityActionMessage(PointerInputCmdHandler.PointerInputCmdArgs args)
#pragma warning restore 618
        {
            return new PerformTargetEntityItemActionMessage(_action.ActionType, _item.Uid, args.EntityUid);
        }

        public override string ToString()
        {
            return $"{nameof(_action)}: {_action.ActionType}, {nameof(_item)}: {_item}";
        }
    }
}
