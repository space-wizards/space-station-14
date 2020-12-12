#nullable enable
using System.Collections.Generic;
using Content.Client.GameObjects.Components.HUD.Inventory;
using Content.Client.GameObjects.Components.Items;
using Content.Client.GameObjects.Components.Mobs.Actions;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <inheritdoc/>
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ClientActionsComponent : SharedActionsComponent
    {
        public const byte Hotbars = 10;
        public const byte Slots = 10;

        [Dependency] private readonly IPlayerManager _playerManager = default!;

        [ComponentDependency] private readonly HandsComponent? _handsComponent = null;
        [ComponentDependency] private readonly ClientInventoryComponent? _inventoryComponent = null;

        private ActionsUI? _ui;
        private ActionMenu? _menu;
        private readonly List<ItemSlotButton> _highlightingItemSlots = new();
        // tracks the action slot we are currently selecting a target for
        private ActionSlot? _selectingTargetFor;

        private readonly ActionAssignments _assignments = new(Hotbars, Slots);

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        [ViewVariables]
        private bool CurrentlyControlled => _playerManager.LocalPlayer != null && _playerManager.LocalPlayer.ControlledEntity == Owner;

        // index of currently displayed hotbar
        private byte _selectedHotbar;

        protected override void Shutdown()
        {
            base.Shutdown();
            PlayerDetached();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PlayerAttachedMsg _:
                    PlayerAttached();
                    break;
                case PlayerDetachedMsg _:
                    PlayerDetached();
                    break;
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not ActionComponentState)
            {
                return;
            }

            UpdateUI();
        }

        private void PlayerAttached()
        {
            if (!CurrentlyControlled || _ui != null)
            {
                return;
            }

            _ui = new ActionsUI(OnActionPress,
                OnActionSlotDragDrop, OnMouseEnteredAction, OnMouseExitedAction,
                NextHotbar, PreviousHotbar, HandleOpenActionMenu);
            _menu = new ActionMenu(this, ActionMenuItemSelected,
                ActionMenuItemDragDropped);

            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(_ui);

            UpdateUI();
        }

        private void PlayerDetached()
        {
            StopTargeting();
            if (_menu != null)
            {
                _menu.Close();
                _menu = null;
            }
            if (_ui != null)
            {
                IoCManager.Resolve<IUserInterfaceManager>().StateRoot.RemoveChild(_ui);
                _ui = null;
            }
        }

        public void HandleHotbarKeybind(byte slot, in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            _ui?.HandleHotbarKeybind(slot, args);
        }

        /// <summary>
        /// Updates the displayed hotbar (and menu) based on current state of actions.
        /// </summary>
        private void UpdateUI()
        {
            if (!CurrentlyControlled || _ui == null)
            {
                return;
            }

            _menu?.UpdateUI();

            _assignments.Reconcile(_selectedHotbar, ActionStates(), ItemActionStates());

            // now update the controls of only the current selected hotbar.
            foreach (var actionSlot in _ui.Slots)
            {
                var assignedActionType = _assignments[_selectedHotbar, actionSlot.SlotIndex];
                if (!assignedActionType.HasValue)
                {
                    actionSlot.Clear();
                    continue;
                }

                if (assignedActionType.Value.TryGetAction(out var actionType))
                {
                    UpdateActionSlot(actionType, actionSlot, assignedActionType);
                }
                else if (assignedActionType.Value.TryGetItemActionWithoutItem(out var itemlessActionType))
                {
                    UpdateActionSlot(itemlessActionType, actionSlot, assignedActionType);
                }
                else if (assignedActionType.Value.TryGetItemActionWithItem(out var itemActionType, out var item))
                {
                    UpdateActionSlot(item, itemActionType, actionSlot, assignedActionType);
                }
                else
                {
                    Logger.WarningS("action", "unexpected Assignment type {0}",
                        assignedActionType.Value.Assignment);
                    actionSlot.Clear();
                }
            }
        }

        private void UpdateActionSlot(ActionType actionType, ActionSlot actionSlot, ActionAssignment? assignedActionType)
        {
            if (ActionManager.TryGet(actionType, out var action))
            {
                actionSlot.Assign(action, true);
            }
            else
            {
                Logger.WarningS("action", "unrecognized actionType {0}", assignedActionType);
                actionSlot.Clear();
                return;
            }

            if (!TryGetActionState(actionType, out var actionState) || !actionState.Enabled)
            {
                // action is currently disabled

                // just revoked an action we were trying to target with, stop targeting
                if (_selectingTargetFor?.Action != null && _selectingTargetFor.Action == action)
                {
                    StopTargeting();
                }

                actionSlot.DisableAction();
                actionSlot.Cooldown = null;
            }
            else
            {
                // action is currently granted
                actionSlot.EnableAction();
                actionSlot.Cooldown = actionState.Cooldown;

                // if we are targeting with an action now on cooldown, stop targeting
                if (_selectingTargetFor?.Action != null && _selectingTargetFor.Action == action &&
                    actionState.IsOnCooldown(GameTiming))
                {
                    StopTargeting();
                }
            }

            // check if we need to toggle it
            if (action.BehaviorType == BehaviorType.Toggle)
            {
                actionSlot.ToggledOn = actionState.ToggledOn;
            }
        }

        private void UpdateActionSlot(ItemActionType itemlessActionType, ActionSlot actionSlot,
            ActionAssignment? assignedActionType)
        {
            if (ActionManager.TryGet(itemlessActionType, out var action))
            {
                actionSlot.Assign(action);
            }
            else
            {
                Logger.WarningS("action", "unrecognized actionType {0}", assignedActionType);
                actionSlot.Clear();
            }
            actionSlot.Cooldown = null;
        }

        private void UpdateActionSlot(EntityUid item, ItemActionType itemActionType, ActionSlot actionSlot,
            ActionAssignment? assignedActionType)
        {
            if (!EntityManager.TryGetEntity(item, out var itemEntity)) return;
            if (ActionManager.TryGet(itemActionType, out var action))
            {
                actionSlot.Assign(action, itemEntity, true);
            }
            else
            {
                Logger.WarningS("action", "unrecognized actionType {0}", assignedActionType);
                actionSlot.Clear();
                return;
            }

            if (!TryGetItemActionState(itemActionType, item, out var actionState))
            {
                // action is no longer tied to an item, this should never happen as we
                // check this at the start of this method. But just to be safe
                // we will restore our assignment here to the correct state
                Logger.WarningS("action", "coding error, expected actionType {0} to have" +
                                          " a state but it didn't", assignedActionType);
                _assignments.AssignSlot(_selectedHotbar, actionSlot.SlotIndex,
                    ActionAssignment.For(itemActionType));
                actionSlot.Assign(action);
                return;
            }

            if (!actionState.Enabled)
            {
                // just disabled an action we were trying to target with, stop targeting
                if (_selectingTargetFor?.Action != null && _selectingTargetFor.Action == action)
                {
                    StopTargeting();
                }

                actionSlot.DisableAction();
            }
            else
            {
                // action is currently granted
                actionSlot.EnableAction();

                // if we are targeting with an action now on cooldown, stop targeting
                if (_selectingTargetFor?.Action != null && _selectingTargetFor.Action == action &&
                    _selectingTargetFor.Item == itemEntity &&
                    actionState.IsOnCooldown(GameTiming))
                {
                    StopTargeting();
                }
            }
            actionSlot.Cooldown = actionState.Cooldown;

            // check if we need to toggle it
            if (action.BehaviorType == BehaviorType.Toggle)
            {
                actionSlot.ToggledOn = actionState.ToggledOn;
            }
        }

        private void NextHotbar(BaseButton.ButtonEventArgs args)
        {
            ChangeHotbar((byte) ((_selectedHotbar + 1) % Hotbars));
        }

        private void PreviousHotbar(BaseButton.ButtonEventArgs args)
        {
            var newBar = _selectedHotbar == 0 ? Hotbars - 1 : _selectedHotbar - 1;
            ChangeHotbar((byte) newBar);
        }

        private void ChangeHotbar(byte hotbar)
        {
            StopTargeting();
            _selectedHotbar = hotbar;
            _ui?.SetHotbarLabel(hotbar + 1);

            UpdateUI();
        }

        private void HandleOpenActionMenu(BaseButton.ButtonEventArgs args)
        {
            ToggleActionsMenu();
        }

        public void ToggleActionsMenu()
        {
            if (_menu == null) return;
            if (_menu.IsOpen)
            {
                _menu.Close();
            }
            else
            {
                _menu.OpenCentered();
            }
        }

        private void ActionMenuItemSelected(ActionMenuItemSelectedEventArgs args)
        {
            switch (args.Action)
            {
                case ActionPrototype actionPrototype:
                    _assignments.AutoPopulate(ActionAssignment.For(actionPrototype.ActionType), _selectedHotbar);
                    break;
                case ItemActionPrototype itemActionPrototype:
                    _assignments.AutoPopulate(ActionAssignment.For(itemActionPrototype.ActionType), _selectedHotbar);
                    break;
                default:
                    Logger.WarningS("action", "unexpected action prototype {0}", args.Action);
                    break;
            }

            UpdateUI();
        }

        private void OnActionPress(BaseButton.ButtonEventArgs args)
        {
            if (_ui == null) return;
            if (_ui.IsDragging) return;
            if (args.Button is not ActionSlot actionSlot) return;
            if (!actionSlot.HasAssignment) return;

            if (args.Event.Function == EngineKeyFunctions.UIRightClick)
            {
                // right click to clear the action
                if (_ui.Locked) return;
                _assignments.ClearSlot(_selectedHotbar, actionSlot.SlotIndex, true);

                StopTargeting();
                actionSlot.Clear();
                return;
            }

            if (args.Event.Function != EngineKeyFunctions.Use &&
                args.Event.Function != EngineKeyFunctions.UIClick) return;

            // no left-click interaction with it on cooldown or revoked
            if (!actionSlot.ActionEnabled || actionSlot.IsOnCooldown || actionSlot.Action == null) return;

            var attempt = ActionAttempt(actionSlot);
            if (attempt == null) return;

            switch (attempt.Action.BehaviorType)
            {
                case BehaviorType.Instant:
                    // for instant actions, we immediately tell the server we're doing it
                    SendNetworkMessage(attempt.PerformInstantActionMessage());
                    break;
                case BehaviorType.Toggle:
                    // for toggle actions, we immediately tell the server we're toggling it.
                    // Predictive toggle it on as well
                    if (attempt.TryGetActionState(this, out var actionState))
                    {
                        actionSlot.ToggledOn = !actionState.ToggledOn;
                        attempt.ToggleAction(this, !actionState.ToggledOn);
                        SendNetworkMessage(attempt.PerformToggleActionMessage(!actionState.ToggledOn));
                    }
                    else
                    {
                        Logger.ErrorS("action", "attempted to toggle action {0} which has" +
                                                  " unknown state", attempt);
                    }

                    break;
                case BehaviorType.TargetPoint:
                case BehaviorType.TargetEntity:
                    // for target actions, we go into "select target" mode, we don't
                    // message the server until we actually pick our target.

                    // if we're clicking the same thing we're already targeting for, then we simply cancel
                    // targeting
                    if (_selectingTargetFor == actionSlot)
                    {
                        StopTargeting();
                        break;
                    }

                    StartTargeting(actionSlot);
                    break;
                case BehaviorType.None:
                    break;
                default:
                    Logger.ErrorS("action", "unhandled action press for action {0}",
                        attempt);
                    break;
            }
        }

        /// <summary>
        /// Handles clicks when selecting the target for an action. Only has an effect when currently
        /// selecting a target.
        /// </summary>
        public bool TargetingOnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            // not currently predicted
            if (EntitySystem.Get<InputSystem>().Predicted) return false;

            // only do something for actual target-based actions
            if (_selectingTargetFor?.Action == null ||
                (_selectingTargetFor.Action.BehaviorType != BehaviorType.TargetEntity &&
                _selectingTargetFor.Action.BehaviorType != BehaviorType.TargetPoint)) return false;

            var attempt = ActionAttempt(_selectingTargetFor);
            if (attempt == null)
            {
                StopTargeting();
                return false;
            }

            switch (_selectingTargetFor.Action.BehaviorType)
            {
                case BehaviorType.TargetPoint:
                {
                    // send our action to the server, we chose our target
                    SendNetworkMessage(attempt.PerformTargetPointActionMessage(args));
                    if (!attempt.Action.Repeat)
                    {
                        StopTargeting();
                    }
                    return true;
                }
                // target the currently hovered entity, if there is one
                case BehaviorType.TargetEntity when args.EntityUid != EntityUid.Invalid:
                {
                    // send our action to the server, we chose our target
                    SendNetworkMessage(attempt.PerformTargetEntityActionMessage(args));
                    if (!attempt.Action.Repeat)
                    {
                        StopTargeting();
                    }
                    return true;
                }
                default:
                    StopTargeting();
                    return false;
            }
        }

        /// <summary>
        /// Action attempt for performing the action in the slot
        /// </summary>
        private static IActionAttempt? ActionAttempt(ActionSlot actionSlot)
        {
            IActionAttempt? attempt = actionSlot.Action switch
            {
                ActionPrototype actionPrototype => new ActionAttempt(actionPrototype),
                ItemActionPrototype itemActionPrototype =>
                    actionSlot.Item != null ? new ItemActionAttempt(itemActionPrototype, actionSlot.Item) : null,
                _ => null
            };
            return attempt;
        }

        private void OnActionSlotDragDrop(ActionSlotDragDropEventArgs obj)
        {
            // swap the 2 slots
            var fromIdx = obj.FromSlot.SlotIndex;
            var fromAssignment = _assignments[_selectedHotbar, fromIdx];
            var toIdx = obj.ToSlot.SlotIndex;
            var toAssignment = _assignments[_selectedHotbar, toIdx];

            if (fromIdx == toIdx) return;
            if (!fromAssignment.HasValue) return;

            _assignments.AssignSlot(_selectedHotbar, toIdx, fromAssignment.Value);
            if (toAssignment.HasValue)
            {
                _assignments.AssignSlot(_selectedHotbar, fromIdx, toAssignment.Value);
            }
            else
            {
                _assignments.ClearSlot(_selectedHotbar, fromIdx, false);
            }

            UpdateUI();
        }

        private void ActionMenuItemDragDropped(ActionMenuItemDragDropEventArgs obj)
        {
            switch (obj.ActionMenuItem.Action)
            {
                // assign the dragged action to the target slot
                case ActionPrototype actionPrototype:
                    _assignments.AssignSlot(_selectedHotbar, obj.ToSlot.SlotIndex, ActionAssignment.For(actionPrototype.ActionType));
                    break;
                case ItemActionPrototype itemActionPrototype:
                    // the action menu doesn't show us if the action has an associated item,
                    // so when we perform the assignment, we should check if we currently have an unassigned state
                    // for this item and assign it tied to that item if so, otherwise assign it "itemless"

                    // this is not particularly efficient but we don't maintain an index from
                    // item action type to its action states, and this method should be pretty infrequent so it's probably fine
                    var assigned = false;
                    foreach (var (item, itemStates) in ItemActionStates())
                    {
                        foreach (var (actionType, _) in itemStates)
                        {
                            if (actionType != itemActionPrototype.ActionType) continue;
                            var assignment = ActionAssignment.For(actionType, item);
                            if (_assignments.HasAssignment(assignment)) continue;
                            // no assignment for this state, assign tied to the item
                            assigned = true;
                            _assignments.AssignSlot(_selectedHotbar, obj.ToSlot.SlotIndex, assignment);
                            break;
                        }

                        if (assigned)
                        {
                            break;
                        }
                    }

                    if (!assigned)
                    {
                        _assignments.AssignSlot(_selectedHotbar, obj.ToSlot.SlotIndex, ActionAssignment.For(itemActionPrototype.ActionType));
                    }
                    break;
            }

            UpdateUI();
        }

        private void OnMouseEnteredAction(GUIMouseHoverEventArgs args)
        {
            // highlight the inventory slot associated with this if it's an item action
            // tied to an item
            if (args.SourceControl is not ActionSlot actionSlot) return;
            if (actionSlot.Action is not ItemActionPrototype) return;
            if (actionSlot.Item == null) return;

            StopHighlightingItemSlots();

            // figure out if it's in hand or inventory and highlight it
            foreach (var hand in _handsComponent!.Hands)
            {
                if (hand.Entity == actionSlot.Item && hand.Button != null)
                {
                    _highlightingItemSlots.Add(hand.Button);
                    hand.Button.Highlight(true);
                    return;
                }
            }

            foreach (var (slot, item) in _inventoryComponent!.AllSlots)
            {
                if (item != actionSlot.Item) continue;
                foreach (var itemSlotButton in
                    _inventoryComponent.InterfaceController.GetItemSlotButtons(slot))
                {
                    _highlightingItemSlots.Add(itemSlotButton);
                    itemSlotButton.Highlight(true);
                }
                return;
            }

        }

        private void OnMouseExitedAction(GUIMouseHoverEventArgs args)
        {
            StopHighlightingItemSlots();
        }

        private void StopHighlightingItemSlots()
        {
            foreach (var itemSlot in _highlightingItemSlots)
            {
                itemSlot.Highlight(false);
            }
            _highlightingItemSlots.Clear();
        }

        /// <summary>
        /// Puts us in targeting mode, where we need to pick either a target point or entity
        /// </summary>
        private void StartTargeting(ActionSlot actionSlot)
        {
            // If we were targeting something else we should stop
            StopTargeting();

            _selectingTargetFor = actionSlot;

            // show it as toggled on to indicate we are currently selecting a target for it
            if (!actionSlot.ToggledOn)
            {
                actionSlot.ToggledOn = true;
            }
        }

        /// <summary>
        /// Switch out of targeting mode if currently selecting target for an action
        /// </summary>
        private void StopTargeting()
        {
            if (_selectingTargetFor != null)
            {
                if (_selectingTargetFor.ToggledOn)
                {
                    _selectingTargetFor.ToggledOn = false;
                }
                _selectingTargetFor = null;
            }
        }

        protected override void AfterActionChanged()
        {
            UpdateUI();
        }
    }
}
