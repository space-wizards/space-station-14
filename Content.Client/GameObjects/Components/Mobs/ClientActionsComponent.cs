using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.HUD.Inventory;
using Content.Client.GameObjects.Components.Items;
using Content.Client.GameObjects.Components.Mobs.Actions;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <inheritdoc/>
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ClientActionsComponent : SharedActionsComponent
    {
        public static readonly byte Hotbars = 10;
        public static readonly byte Slots = 10;
        private static readonly float TooltipTextMaxWidth = 350;

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private HandsComponent _handsComponent;
        private ClientInventoryComponent _inventoryComponent;

        private ActionsUI _ui;
        private ActionMenu _menu;
        private PanelContainer _tooltip;
        private RichTextLabel _actionName;
        private RichTextLabel _actionDescription;
        private RichTextLabel _actionCooldown;
        private RichTextLabel _actionRequirements;
        private bool _tooltipReady;
        private ActionSlot _showingTooltipFor;
        private List<ItemSlotButton> _highlightingItemSlots = new List<ItemSlotButton>();
        // so we don't call it every frame and only update the text each second that ticks
        private int _tooltipCooldownSecs = -1;
        // tracks the action slot we are currently selecting a target for
        private ActionSlot _selectingTargetFor;

        private readonly ActionAssignments _assignments = new ActionAssignments(Hotbars, Slots);

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// TODO: should be revisited after space-wizards/RobustToolbox#1255
        /// </summary>
        [ViewVariables]
        private bool CurrentlyControlled => _playerManager.LocalPlayer != null && _playerManager.LocalPlayer.ControlledEntity == Owner;

        // index of currently displayed hotbar
        private byte _selectedHotbar = 0;

        protected override void Shutdown()
        {
            base.Shutdown();
            PlayerDetached();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
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

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is ActionComponentState))
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

            _ui = new ActionsUI(ActionOnOnShowTooltip, ActionOnOnHideTooltip, OnActionPress,
                OnActionSlotDragDrop, OnMouseEnteredAction, OnMouseExitedAction,
                NextHotbar,
                PreviousHotbar, HandleOpenActionMenu);
            _menu = new ActionMenu(ActionOnOnShowTooltip, ActionOnOnHideTooltip, this, ActionMenuItemSelected,
                ActionMenuItemDragDropped);

            var uiManager = IoCManager.Resolve<IUserInterfaceManager>();
            uiManager.StateRoot.AddChild(_ui);

            _inventoryComponent = Owner.GetComponent<ClientInventoryComponent>();
            _handsComponent = Owner.GetComponent<HandsComponent>();

            _tooltip = new PanelContainer
            {
                Visible = false,
                StyleClasses = { StyleNano.StyleClassTooltipPanel }
            };
            var tooltipVBox = new VBoxContainer
            {
                RectClipContent = true
            };
            _tooltip.AddChild(tooltipVBox);
            _actionName = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipActionTitle }
            };
            tooltipVBox.AddChild(_actionName);
            _actionDescription = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipActionDescription }
            };
            tooltipVBox.AddChild(_actionDescription);
            _actionCooldown = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipActionCooldown }
            };
            tooltipVBox.AddChild(_actionCooldown);
            _actionRequirements = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipActionRequirements }
            };
            tooltipVBox.AddChild(_actionRequirements);

            uiManager.PopupRoot.AddChild(_tooltip);

            // set up hotkeys for hotbar
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenActionsMenu,
                    InputCmdHandler.FromDelegate(s => ToggleActionsMenu()))
                .Bind(ContentKeyFunctions.Hotbar1,
                    HandleHotbarKeybind(0))
                .Bind(ContentKeyFunctions.Hotbar2,
                    HandleHotbarKeybind(1))
                .Bind(ContentKeyFunctions.Hotbar3,
                    HandleHotbarKeybind(2))
                .Bind(ContentKeyFunctions.Hotbar4,
                    HandleHotbarKeybind(3))
                .Bind(ContentKeyFunctions.Hotbar5,
                    HandleHotbarKeybind(4))
                .Bind(ContentKeyFunctions.Hotbar6,
                    HandleHotbarKeybind(5))
                .Bind(ContentKeyFunctions.Hotbar7,
                    HandleHotbarKeybind(6))
                .Bind(ContentKeyFunctions.Hotbar8,
                    HandleHotbarKeybind(7))
                .Bind(ContentKeyFunctions.Hotbar9,
                    HandleHotbarKeybind(8))
                .Bind(ContentKeyFunctions.Hotbar0,
                    HandleHotbarKeybind(9))
                // when selecting a target, we intercept clicks in the game world, treating them as our target selection. We want to
                // take priority before any other systems handle the click.
                .BindBefore(EngineKeyFunctions.Use, new PointerInputCmdHandler(TargetingOnUse),
                    typeof(ConstructionSystem), typeof(DragDropSystem))
                .Register<ClientActionsComponent>();

            UpdateUI();
        }

        private PointerInputCmdHandler HandleHotbarKeybind(byte slot)
        {
            // delegate to the ActionsUI, simulating a click on it
            return new PointerInputCmdHandler((in PointerInputCmdHandler.PointerInputCmdArgs args) =>
                {
                    _ui.HandleHotbarKeybind(slot, args);
                    return true;
                },
                false);
        }

        private void PlayerDetached()
        {
            StopTargeting();
            CommandBinds.Unregister<ClientActionsComponent>();
            _menu?.Dispose();
            _ui?.Dispose();
            _ui = null;
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

            _assignments.Reconcile(_selectedHotbar, EnumerateActionStates(), EnumerateItemActions());

            // now update the controls of only the current selected hotbar.
            foreach (var actionSlot in _ui.Slots)
            {
                var assignedActionType = _assignments[_selectedHotbar, actionSlot.SlotIndex];
                if (!assignedActionType.HasValue)
                {
                    actionSlot.Clear();
                    continue;
                }

                switch (assignedActionType.Value.Assignment)
                {
                    case Assignment.Action:
                    {
                        if (!assignedActionType.Value.ActionType.HasValue)
                        {
                            // should never happen
                            actionSlot.Clear();
                            continue;
                        }
                        var actionType = assignedActionType.Value.ActionType.Value;
                        if (ActionManager.TryGet(actionType, out var action))
                        {
                            actionSlot.Assign(action, true);
                        }
                        else
                        {
                            Logger.WarningS("action", "unrecognized actionType {0}", assignedActionType);
                            actionSlot.Clear();
                            continue;
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
                        }
                        else
                        {
                            // action is currently granted
                            actionSlot.EnableAction();

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
                        break;
                    }
                    case Assignment.ItemActionWithoutItem:
                    {
                        if (!assignedActionType.Value.ItemActionType.HasValue)
                        {
                            // should never happen
                            actionSlot.Clear();
                            continue;
                        }

                        var actionType = assignedActionType.Value.ItemActionType.Value;
                        if (ActionManager.TryGet(actionType, out var action))
                        {
                            actionSlot.Assign(action);
                        }
                        else
                        {
                            Logger.WarningS("action", "unrecognized actionType {0}", assignedActionType);
                            actionSlot.Clear();
                            continue;
                        }
                        break;
                    }
                    case Assignment.ItemActionWithItem:
                    {
                        if (!assignedActionType.Value.ItemActionType.HasValue || !assignedActionType.Value.Item.HasValue)
                        {
                            // should never happen
                            actionSlot.Clear();
                            continue;
                        }
                        var actionType = assignedActionType.Value.ItemActionType.Value;
                        var item = EntityManager.GetEntity(assignedActionType.Value.Item.Value);
                        if (ActionManager.TryGet(actionType, out var action))
                        {
                            actionSlot.Assign(action, item, true);
                        }
                        else
                        {
                            Logger.WarningS("action", "unrecognized actionType {0}", assignedActionType);
                            actionSlot.Clear();
                            continue;
                        }

                        if (!TryGetItemActionState(actionType, item.Uid, out var actionState))
                        {
                            // action is no longer tied to an item, this should never happen as we
                            // check this at the start of this method. But just to be safe
                            // we will restore our assignment here to the correct state
                            Logger.WarningS("action", "coding error, expected actionType {0} to have" +
                                                      " a state but it didn't", assignedActionType);
                            _assignments.AssignSlot(_selectedHotbar, actionSlot.SlotIndex,
                                ActionAssignment.For(assignedActionType.Value.ItemActionType.Value));
                            actionSlot.Assign(action);
                            continue;
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
                                _selectingTargetFor.Item == item &&
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
                        break;
                    }
                    default:
                    {
                        Logger.WarningS("action", "unexpected Assignment type {0}",
                            assignedActionType.Value.Assignment);
                        actionSlot.Clear();
                        break;
                    }
                }
            }
        }

        private void NextHotbar(BaseButton.ButtonEventArgs args)
        {
            ChangeHotbar((byte) ((_selectedHotbar + 1) % Hotbars));
        }

        private void PreviousHotbar(BaseButton.ButtonEventArgs args)
        {
            int newBar = _selectedHotbar == 0 ? Hotbars - 1 : _selectedHotbar - 1;
            ChangeHotbar((byte) newBar);
        }

        private void ChangeHotbar(byte hotbar)
        {
            StopTargeting();
            _selectedHotbar = hotbar;
            _ui.SetHotbarLabel(hotbar + 1);

            UpdateUI();
        }

        private void HandleOpenActionMenu(BaseButton.ButtonEventArgs args)
        {
            ToggleActionsMenu();
        }

        private void ToggleActionsMenu()
        {
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
            if (_ui.IsDragging) return;
            if (!(args.Button is ActionSlot actionSlot)) return;
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

            if (args.Event.Function != EngineKeyFunctions.Use && args.Event.Function != EngineKeyFunctions.UIClick) return;

            // no left-click interaction with it on cooldown or revoked
            if (!actionSlot.ActionEnabled || actionSlot.IsOnCooldown) return;

            switch (actionSlot.Action)
            {
                case ActionPrototype actionPrototype:
                    HandleActionPress(actionSlot, actionPrototype);
                    break;
                case ItemActionPrototype itemActionPrototype:
                    HandleItemActionPress(actionSlot, itemActionPrototype);
                    break;
            }
        }

        private void HandleActionPress(ActionSlot actionSlot, ActionPrototype action)
        {
            switch (action.BehaviorType)
            {
                case BehaviorType.Instant:
                    // for instant actions, we immediately tell the server we're doing it
                    SendNetworkMessage(new PerformInstantActionMessage(action.ActionType));
                    break;
                case BehaviorType.Toggle:
                    // for toggle actions, we immediately tell the server we're toggling it.
                    // Predictively toggle it on as well
                    if (TryGetActionState(action.ActionType, out var actionState))
                    {
                        actionSlot.ToggledOn = !actionState.ToggledOn;
                        // TODO: This flickers when toggling on due to ResetPredictedEntities being
                        // called with an older (toggled off) state from the server.
                        ToggleAction(action.ActionType, !actionState.ToggledOn);
                        SendNetworkMessage(new PerformToggleActionMessage(action.ActionType,
                            !actionState.ToggledOn));
                    }
                    else
                    {
                        Logger.WarningS("action", "attempted to toggle action {0} which has" +
                                                  " unknown state", action.ActionType);
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
                default:
                    Logger.WarningS("action", "unhandled action press for action {0}", action.ActionType);
                    break;
            }
        }

         private void HandleItemActionPress(ActionSlot actionSlot, ItemActionPrototype action)
        {
            if (actionSlot.Item == null) return;
            switch (action.BehaviorType)
            {
                case BehaviorType.Instant:
                    // for instant actions, we immediately tell the server we're doing it
                    SendNetworkMessage(new PerformInstantItemActionMessage(action.ActionType, actionSlot.Item.Uid));
                    break;
                case BehaviorType.Toggle:
                    // for toggle actions, we immediately tell the server we're toggling it.
                    // Predictively toggle it on as well
                    if (TryGetItemActionState(action.ActionType, actionSlot.Item.Uid, out var actionState))
                    {
                        actionSlot.ToggledOn = !actionState.ToggledOn;
                        // TODO: This flickers when toggling on due to ResetPredictedEntities being
                        // called with an older (toggled off) state from the server.
                        ToggleAction(action.ActionType, actionSlot.Item.Uid, !actionState.ToggledOn);
                        SendNetworkMessage(new PerformToggleItemActionMessage(action.ActionType,
                            actionSlot.Item.Uid, !actionState.ToggledOn));
                    }
                    else
                    {
                        Logger.WarningS("action", "attempted to toggle item action {0} which has" +
                                                  " unknown state", action.ActionType);
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
                default:
                    Logger.WarningS("action", "unhandled action press for item action {0}", action.ActionType);
                    break;
            }
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
                    foreach (var (item, itemStates) in EnumerateItemActions())
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
            if (!(args.SourceControl is ActionSlot actionSlot)) return;
            if (!(actionSlot.Action is ItemActionPrototype itemActionPrototype)) return;
            if (actionSlot.Item == null) return;

            StopHighlightingItemSlots();

            // figure out if it's in hand or inventory and highlight it
            foreach (var hand in _handsComponent.Hands)
            {
                if (hand.Entity == actionSlot.Item && hand.Button != null)
                {
                    _highlightingItemSlots.Add(hand.Button);
                    hand.Button.Highlight(true);
                    return;
                }
            }

            foreach (var slot in _inventoryComponent.AllSlots)
            {
                if (slot.Value == actionSlot.Item)
                {
                    if (_inventoryComponent.InterfaceController == null) return;
                    foreach (var itemSlotButton in
                        _inventoryComponent.InterfaceController.GetItemSlotButtons(slot.Key))
                    {
                        _highlightingItemSlots.Add(itemSlotButton);
                        itemSlotButton.Highlight(true);
                    }
                    return;
                }
            }

        }

        private void OnMouseExitedAction(GUIMouseHoverEventArgs args)
        {
            StopHighlightingItemSlots();
        }

        private void StopHighlightingItemSlots()
        {
            if (_highlightingItemSlots == null) return;
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

        private bool TargetingOnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            // not currently predicted
            if (EntitySystem.Get<InputSystem>().Predicted) return false;

            // only do something for actual target-based actions
            if (_selectingTargetFor?.Action == null ||
                (_selectingTargetFor.Action.BehaviorType != BehaviorType.TargetEntity &&
                _selectingTargetFor.Action.BehaviorType != BehaviorType.TargetPoint)) return false;

            switch (_selectingTargetFor.Action)
            {
                case ActionPrototype actionPrototype:
                    return HandleTargetingOnUse(args, actionPrototype);
                case ItemActionPrototype itemActionPrototype:
                    return HandleTargetingOnUse(args, itemActionPrototype);
                default:
                    StopTargeting();
                    return false;
            }
        }

        private bool HandleTargetingOnUse(PointerInputCmdHandler.PointerInputCmdArgs args,
            ActionPrototype action)
        {
            if (_selectingTargetFor.Action.BehaviorType == BehaviorType.TargetPoint)
            {
                // send our action to the server, we chose our target
                SendNetworkMessage(new PerformTargetPointActionMessage(action.ActionType,
                    args.Coordinates));
                if (!_selectingTargetFor.Action.Repeat)
                {
                    StopTargeting();
                }

                return true;
            }

            if (_selectingTargetFor.Action.BehaviorType == BehaviorType.TargetEntity)
            {
                // target the currently hovered entity, if there is one
                if (args.EntityUid != EntityUid.Invalid)
                {
                    // send our action to the server, we chose our target
                    SendNetworkMessage(new PerformTargetEntityActionMessage(action.ActionType,
                        args.EntityUid));
                    if (!_selectingTargetFor.Action.Repeat)
                    {
                        StopTargeting();
                    }

                    return true;
                }
            }

            StopTargeting();
            return false;
        }

        private bool HandleTargetingOnUse(PointerInputCmdHandler.PointerInputCmdArgs args, ItemActionPrototype action)
        {
            if (_selectingTargetFor.Item == null) return false;
            if (_selectingTargetFor.Action.BehaviorType == BehaviorType.TargetPoint)
            {
                // send our action to the server, we chose our target
                SendNetworkMessage(new PerformTargetPointItemActionMessage(action.ActionType,
                    _selectingTargetFor.Item.Uid, args.Coordinates));
                if (!_selectingTargetFor.Action.Repeat)
                {
                    StopTargeting();
                }

                return true;
            }

            if (_selectingTargetFor.Action.BehaviorType == BehaviorType.TargetEntity)
            {
                // target the currently hovered entity, if there is one
                if (args.EntityUid != EntityUid.Invalid)
                {
                    // send our action to the server, we chose our target
                    SendNetworkMessage(new PerformTargetEntityItemActionMessage(action.ActionType,
                        _selectingTargetFor.Item.Uid,
                        args.EntityUid));
                    if (!_selectingTargetFor.Action.Repeat)
                    {
                        StopTargeting();
                    }

                    return true;
                }
            }

            StopTargeting();
            return false;
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

        private void ActionOnOnHideTooltip(object sender, EventArgs e)
        {
            _tooltipReady = false;
            _tooltip.Visible = false;
            _showingTooltipFor = null;
        }

        private void ActionOnOnShowTooltip(object sender, EventArgs e)
        {
            // this can come from an ActionSlot or an ActionMenuItem depending on if its for the
            // action hotbar or the action menu

            BaseActionPrototype action = null;
            var totalCooldownDuration = TimeSpan.Zero;
            var cooldownRemaining = TimeSpan.Zero;
            if (sender is ActionSlot actionSlot)
            {
                action = actionSlot.Action;
                totalCooldownDuration = actionSlot.TotalDuration;
                cooldownRemaining = actionSlot.CooldownRemaining;
                _showingTooltipFor = actionSlot;
            }
            else if (sender is ActionMenuItem actionMenuItem)
            {
                action = actionMenuItem.Action;
                // TODO: We can't report cooldowns in the action menu
                // because they are currently set on-demand.
            }
            else
            {
                // coding error, we got an unexpected sender
                throw new InvalidOperationException();
            }

            if (action == null)
            {
                _showingTooltipFor = null;
                return;
            }

            _actionName.SetMessage(action.Name);
            _actionDescription.SetMessage(action.Description);
            // check for a cooldown
            _tooltipCooldownSecs = -1;
            UpdateTooltipCooldown(cooldownRemaining, totalCooldownDuration);
            //check for requirements message
            if (action.Requires != null)
            {
                _actionRequirements.SetMessage(FormattedMessage.FromMarkup("[color=#635c5c]" +
                                                                           action.Requires +
                                                                           "[/color]"));
                _actionRequirements.Visible = true;
            }
            else
            {
                _actionRequirements.Visible = false;
            }


            Tooltips.PositionTooltip(_tooltip);
            // if we set it visible here the size of the previous tooltip will flicker for a frame,
            // so instead we wait until FrameUpdate to make it visible
            _tooltipReady = true;
        }

        private void UpdateTooltipCooldown(TimeSpan cooldownRemaining, TimeSpan totalDuration)
        {
            if (cooldownRemaining != TimeSpan.Zero)
            {
                if (cooldownRemaining.Seconds == _tooltipCooldownSecs) return;
                _actionCooldown.SetMessage(FormattedMessage.FromMarkup(
                    $"[color=#a10505]{totalDuration.Seconds} sec cooldown ({cooldownRemaining.Seconds + 1} sec remaining)[/color]"));
                _actionCooldown.Visible = true;
                _tooltipCooldownSecs = cooldownRemaining.Seconds;
            }
            else
            {
                _tooltipCooldownSecs = -1;
                _actionCooldown.Visible = false;
            }
        }

        public void FrameUpdate(float frameTime)
        {
            if (_tooltipReady)
            {
                _tooltipReady = false;
                _tooltip.Visible = true;
            }
            // update the cooldowns for each currently displayed hotbar slot.
            // note that we don't actually need to keep track of cooldowns for
            // slots in other hotbars - since we store the precise start and end of each
            // cooldown we have no need to actively tick down, we can always calculate current
            // cooldown amount as-needed (for example when switching toolbars).
            if (_ui == null) return;
            foreach (var actionSlot in _ui.Slots)
            {
                var assignedActionType = _assignments[_selectedHotbar, actionSlot.SlotIndex];
                if (!assignedActionType.HasValue) continue;

                switch (assignedActionType.Value.Assignment)
                {
                    case Assignment.Action:
                    {
                        if (TryGetActionState(assignedActionType.Value.ActionType.Value, out var actionState))
                        {
                            actionSlot.UpdateCooldown(actionState.Cooldown, GameTiming.CurTime);
                        }
                        else
                        {
                            actionSlot.UpdateCooldown(null, GameTiming.CurTime);
                        }
                        break;
                    }
                    case Assignment.ItemActionWithItem:
                    {
                        if (TryGetItemActionState(assignedActionType.Value.ItemActionType.Value,
                            assignedActionType.Value.Item.Value, out var actionState))
                        {
                            actionSlot.UpdateCooldown(actionState.Cooldown, GameTiming.CurTime);
                        }
                        else
                        {
                            actionSlot.UpdateCooldown(null, GameTiming.CurTime);
                        }
                        break;
                    }
                    default:
                    {
                        actionSlot.UpdateCooldown(null, GameTiming.CurTime);
                        break;
                    }
                }

                if (_showingTooltipFor == actionSlot)
                {
                    UpdateTooltipCooldown(actionSlot.CooldownRemaining, actionSlot.TotalDuration);
                }
            }
        }

        protected override void AfterActionChanged()
        {
            UpdateUI();
        }
    }
}
