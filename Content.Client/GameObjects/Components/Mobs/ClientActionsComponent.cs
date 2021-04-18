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
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
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
        public const byte Hotbars = 9;
        public const byte Slots = 10;

        [Dependency] private readonly IPlayerManager _playerManager = default!;

        [ComponentDependency] private readonly HandsComponent? _handsComponent = null;
        [ComponentDependency] private readonly ClientInventoryComponent? _inventoryComponent = null;

        private ActionsUI? _ui;
        private readonly List<ItemSlotButton> _highlightingItemSlots = new();

        /// <summary>
        /// Current assignments for all hotbars / slots for this entity.
        /// </summary>
        public ActionAssignments Assignments { get; } = new(Hotbars, Slots);

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        [ViewVariables]
        private bool CurrentlyControlled => _playerManager.LocalPlayer != null && _playerManager.LocalPlayer.ControlledEntity == Owner;


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

            _ui = new ActionsUI(this);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(_ui);
            UpdateUI();
        }

        private void PlayerDetached()
        {
            if (_ui == null) return;
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.RemoveChild(_ui);
            _ui = null;
        }

        public void HandleHotbarKeybind(byte slot, in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            _ui?.HandleHotbarKeybind(slot, args);
        }

        public void HandleChangeHotbarKeybind(byte hotbar, in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            _ui?.HandleChangeHotbarKeybind(hotbar, args);
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

            Assignments.Reconcile(_ui.SelectedHotbar, ActionStates(), ItemActionStates());

            _ui.UpdateUI();
        }

        public void AttemptAction(ActionSlot slot)
        {

            var attempt = slot.ActionAttempt();
            if (attempt == null) return;

            switch (attempt.Action.BehaviorType)
            {
                case BehaviorType.Instant:
                    // for instant actions, we immediately tell the server we're doing it
                    SendNetworkMessage(attempt.PerformInstantActionMessage());
                    break;
                case BehaviorType.Toggle:
                    // for toggle actions, we immediately tell the server we're toggling it.
                    if (attempt.TryGetActionState(this, out var actionState))
                    {
                        // TODO: At the moment we always predict that the toggle will work clientside,
                        // even if it sometimes may not (it will be reset by the server if wrong).
                        attempt.ToggleAction(this, !actionState.ToggledOn);
                        slot.ToggledOn = !actionState.ToggledOn;
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
                    _ui?.ToggleTargeting(slot);
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
            if (_ui?.SelectingTargetFor?.Action == null ||
                (!_ui.SelectingTargetFor.Action.IsTargetAction)) return false;

            // do nothing if we know it's on cooldown
            if (_ui.SelectingTargetFor.IsOnCooldown) return false;

            var attempt = _ui.SelectingTargetFor.ActionAttempt();
            if (attempt == null)
            {
                _ui.StopTargeting();
                return false;
            }

            switch (_ui.SelectingTargetFor.Action.BehaviorType)
            {
                case BehaviorType.TargetPoint:
                {
                    // send our action to the server, we chose our target
                    SendNetworkMessage(attempt.PerformTargetPointActionMessage(args));
                    if (!attempt.Action.Repeat)
                    {
                        _ui.StopTargeting();
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
                        _ui.StopTargeting();
                    }
                    return true;
                }
                // we are supposed to target an entity but we didn't click it
                case BehaviorType.TargetEntity when args.EntityUid == EntityUid.Invalid:
                {
                    if (attempt.Action.DeselectWhenEntityNotClicked)
                        _ui.StopTargeting();
                    return false;
                }
                default:
                    _ui.StopTargeting();
                    return false;
            }
        }

        protected override void AfterActionChanged()
        {
            UpdateUI();
        }

        /// <summary>
        /// Highlights the item slot (inventory or hand) that contains this item
        /// </summary>
        /// <param name="item"></param>
        public void HighlightItemSlot(IEntity item)
        {
            StopHighlightingItemSlots();

            if (_handsComponent?.Gui == null)
                return;

            // figure out if it's in hand or inventory and highlight it
            foreach (var hand in _handsComponent.Gui.Hands)
            {
                var handButton = hand.HandButton;
                if (hand.HeldItem != item || handButton == null)
                    continue;

                _highlightingItemSlots.Add(handButton);
                handButton.Highlight(true);

                return;
            }

            foreach (var (slot, slotItem) in _inventoryComponent!.AllSlots)
            {
                if (slotItem != item) continue;
                foreach (var itemSlotButton in
                    _inventoryComponent.InterfaceController.GetItemSlotButtons(slot))
                {
                    _highlightingItemSlots.Add(itemSlotButton);
                    itemSlotButton.Highlight(true);
                }
                return;
            }
        }

        /// <summary>
        /// Stops highlighting any item slots we are currently highlighting.
        /// </summary>
        public void StopHighlightingItemSlots()
        {
            foreach (var itemSlot in _highlightingItemSlots)
            {
                itemSlot.Highlight(false);
            }
            _highlightingItemSlots.Clear();
        }

        public void ToggleActionsMenu()
        {
            _ui?.ToggleActionsMenu();
        }
    }
}
