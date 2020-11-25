using System;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Controls;
using Content.Client.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The action hotbar on the left side of the screen.
    /// </summary>
    public sealed class ActionsUI : PanelContainer
    {
        // drag will be triggered when mouse leaves this deadzone around the click position.
        private const float DragDeadzone = 2f;

        private readonly IInputManager _inputManager;

        private readonly EventHandler _onShowTooltip;
        private readonly EventHandler _onHideTooltip;
        private readonly Action<ActionSlotEventArgs> _actionSlotEventHandler;
        private readonly Action<ActionSlotDragDropEventArgs> _actionDragDropEventHandler;
        private readonly Action<BaseButton.ButtonEventArgs> _onNextHotbarPressed;
        private readonly Action<BaseButton.ButtonEventArgs> _onPreviousHotbarPressed;
        private readonly Action<BaseButton.ButtonEventArgs> _onSettingsButtonPressed;
        private readonly ActionSlot[] _slots;

        private readonly VBoxContainer _hotbarContainer;
        private readonly VBoxContainer _slotContainer;

        private readonly TextureButton _lockButton;
        private readonly TextureButton _settingsButton;
        private readonly TextureButton _previousHotbarButton;
        private readonly Label _loadoutNumber;
        private readonly TextureButton _nextHotbarButton;
        private readonly TextureRect _dragShadow;

        private readonly DragDropHelper<ActionSlot> _dragDropHelper;
        public bool IsDragging => _dragDropHelper.IsDragging;

        /// <param name="onShowTooltip">OnShowTooltip handler to assign to each ActionSlot</param>
        /// <param name="onHideTooltip">OnHideTooltip handler to assign to each ActionSlot</param>
        /// <param name="actionSlotEventHandler">handler for interactions with
        /// action slots. Slots with no actions will not be handled by this.</param>
        /// <param name="actionDragDropEventHandler">handler for dragging and dropping from
        /// one slot to another.</param>
        /// <param name="onNextHotbarPressed">action to invoke when pressing the next hotbar button</param>
        /// <param name="onPreviousHotbarPressed">action to invoke when pressing the previous hotbar button</param>
        /// <param name="onSettingsButtonPressed">action to invoke when pressing the settings button</param>
        public ActionsUI(EventHandler onShowTooltip, EventHandler onHideTooltip, Action<ActionSlotEventArgs> actionSlotEventHandler,
            Action<ActionSlotDragDropEventArgs> actionDragDropEventHandler,
            Action<BaseButton.ButtonEventArgs> onNextHotbarPressed, Action<BaseButton.ButtonEventArgs> onPreviousHotbarPressed,
            Action<BaseButton.ButtonEventArgs> onSettingsButtonPressed)
        {
            _inputManager = IoCManager.Resolve<IInputManager>();
            _onShowTooltip = onShowTooltip;
            _onHideTooltip = onHideTooltip;
            _actionSlotEventHandler = actionSlotEventHandler;
            _actionDragDropEventHandler = actionDragDropEventHandler;
            _onNextHotbarPressed = onNextHotbarPressed;
            _onPreviousHotbarPressed = onPreviousHotbarPressed;
            _onSettingsButtonPressed = onSettingsButtonPressed;

            SizeFlagsHorizontal = SizeFlags.FillExpand;
            SizeFlagsVertical = SizeFlags.FillExpand;

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            _hotbarContainer = new VBoxContainer
            {
                SeparationOverride = 3
            };
            AddChild(_hotbarContainer);

            var settingsContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _hotbarContainer.AddChild(settingsContainer);

            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });
            _lockButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/lock.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            settingsContainer.AddChild(_lockButton);
            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            _settingsButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/gear.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            _settingsButton.OnPressed += _onSettingsButtonPressed;
            settingsContainer.AddChild(_settingsButton);
            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });

            _slotContainer = new VBoxContainer();
            _hotbarContainer.AddChild(_slotContainer);

            var loadoutContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _hotbarContainer.AddChild(loadoutContainer);

            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });
            _previousHotbarButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/left_arrow.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            _previousHotbarButton.OnPressed += _onPreviousHotbarPressed;
            loadoutContainer.AddChild(_previousHotbarButton);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            _loadoutNumber = new Label
            {
                Text = "1",
                SizeFlagsStretchRatio = 1
            };
            loadoutContainer.AddChild(_loadoutNumber);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            _nextHotbarButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/right_arrow.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            _nextHotbarButton.OnPressed += _onNextHotbarPressed;
            loadoutContainer.AddChild(_nextHotbarButton);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });

            _slots = new ActionSlot[ClientActionsComponent.Slots];

            _dragShadow = new TextureRect
            {
                CustomMinimumSize = (64, 64),
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false
            };
            UserInterfaceManager.PopupRoot.AddChild(_dragShadow);
            LayoutContainer.SetSize(_dragShadow, (64, 64));

            for (byte i = 1; i <= ClientActionsComponent.Slots; i++)
            {
                var slot = new ActionSlot(i);
                slot.EnableAllKeybinds = true;
                slot.OnShowTooltip += onShowTooltip;
                slot.OnHideTooltip += onHideTooltip;
                slot.OnButtonDown += ActionSlotOnButtonDown;
                slot.OnButtonUp += ActionSlotOnButtonUp;
                slot.OnPressed += ActionSlotOnPressed;
                slot.OnToggled += ActionSlotOnToggled;
                _slotContainer.AddChild(slot);
                _slots[i - 1] = slot;
            }

            _dragDropHelper = new DragDropHelper<ActionSlot>(DragDeadzone, OnBeginActionDrag, OnContinueActionDrag, OnEndActionDrag);
        }

        private bool OnBeginActionDrag()
        {
            // only initiate the drag if the slot has an action in it
            if (_dragDropHelper.Target.Action == null) return false;

            _dragShadow.Texture = _dragDropHelper.Target.Action.Icon.Frame0();
            // don't make visible until frameupdate, otherwise it'll flicker
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled - (32, 32));
            return true;
        }

        private bool OnContinueActionDrag(float frameTime)
        {
            // stop if there's no action in the slot
            if (_dragDropHelper.Target.Action == null) return false;

            // keep dragged entity centered under mouse
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled - (32, 32));
            // we don't set this visible until frameupdate, otherwise it flickers
            _dragShadow.Visible = true;
            return true;
        }

        private void OnEndActionDrag()
        {
            _dragShadow.Visible = false;
        }

        private void ActionSlotOnButtonDown(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function != EngineKeyFunctions.Use) return;
            _dragDropHelper.MouseDown(args.Button as ActionSlot);
        }

        private void ActionSlotOnButtonUp(BaseButton.ButtonEventArgs args)
        {
            // note the buttonup only fires on the control that was originally
            // pressed to initiate the drag, NOT the one we are currently hovering
            if (args.Event.Function != EngineKeyFunctions.Use) return;

            if (UserInterfaceManager.CurrentlyHovered != null &&
                UserInterfaceManager.CurrentlyHovered is ActionSlot targetSlot)
            {
                if (!_dragDropHelper.IsDragging || _dragDropHelper.Target?.Action == null)
                {
                    _dragDropHelper.EndDrag();
                    return;
                }

                // drag and drop
                _actionDragDropEventHandler?.Invoke(new ActionSlotDragDropEventArgs(_dragDropHelper.Target, targetSlot));
            }

            _dragDropHelper.EndDrag();
        }

        private void ActionSlotOnPressed(BaseButton.ButtonEventArgs args)
        {
            if (!(args.Button is ActionSlot actionSlot)) return;
            if (actionSlot.Action == null) return;
            if (args.Event.Function == EngineKeyFunctions.UIRightClick)
            {
                _actionSlotEventHandler.Invoke(new ActionSlotEventArgs(ActionSlotEvent.RightClick, false, actionSlot, args));
                return;
            }
            if (args.Event.Function != EngineKeyFunctions.Use) return;
            if (!actionSlot.Granted) return;
            // only instant actions should be handled as presses, all other actions
            // should be handled as toggles
            if (actionSlot.Action.BehaviorType == BehaviorType.Instant)
            {
                _actionSlotEventHandler.Invoke(new ActionSlotEventArgs(ActionSlotEvent.Press, false, actionSlot, args));
            }
        }

        private void ActionSlotOnToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (!(args.Button is ActionSlot actionSlot)) return;
            if (actionSlot.Action == null) return;
            if (args.Event.Function == EngineKeyFunctions.UIRightClick)
            {
                _actionSlotEventHandler.Invoke(new ActionSlotEventArgs(ActionSlotEvent.RightClick, args.Pressed, actionSlot, args));
                return;
            }
            if (args.Event.Function != EngineKeyFunctions.Use) return;
            if (!actionSlot.Granted) return;
            // only instant actions should be handled as presses, all other actions
            // should be handled as toggles
            if (actionSlot.Action.BehaviorType != BehaviorType.Instant)
            {
                _actionSlotEventHandler.Invoke(new ActionSlotEventArgs(ActionSlotEvent.Toggle, args.Pressed, actionSlot, args));
            }
        }

        /// <summary>
        /// Updates the action assigned to the indicated slot.
        /// </summary>
        /// <param name="slot">slot index to assign to (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        /// <param name="action">action to assign</param>
        public void AssignSlot(byte slot, ActionPrototype action)
        {
            _slots[slot].Assign(action);
        }

        /// <summary>
        /// Clears the action assigned to the indicated slot.
        /// </summary>
        /// <param name="slot">slot index to clear (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        public void ClearSlot(byte slot)
        {
            _slots[slot].Clear();
        }

        /// <summary>
        /// Displays the action on the indicated slot as revoked (makes the number red)
        /// </summary>
        /// <param name="slot">slot index containing the action to revoke (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        public void RevokeSlot(byte slot)
        {
            _slots[slot].Revoke();
        }


        /// <summary>
        /// Displays the action on the indicated slot as granted (makes the number white)
        /// </summary>
        /// <param name="slot">slot index containing the action to revoke (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        public void GrantSlot(byte slot)
        {
            _slots[slot].Grant();
        }

        public void SetHotbarLabel(int number)
        {
            _loadoutNumber.Text = number.ToString();
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _nextHotbarButton.OnPressed -= _onNextHotbarPressed;
            _previousHotbarButton.OnPressed -= _onPreviousHotbarPressed;
            _settingsButton.OnPressed -= _onSettingsButtonPressed;
            foreach (var slot in _slots)
            {
                slot.OnShowTooltip -= _onShowTooltip;
                slot.OnHideTooltip -= _onHideTooltip;
                slot.OnPressed -= ActionSlotOnPressed;
                slot.OnToggled -= ActionSlotOnToggled;
            }
        }

        /// <summary>
        /// Update the cooldown shown on the indicated slot, clearing it if cooldown is null
        /// </summary>
        /// <param name="slot">slot index containing the action to update the cooldown of
        /// (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        /// <param name="cooldown">cooldown start and end time</param>
        /// <param name="curTime">current time</param>
        public void UpdateCooldown(byte slot, (TimeSpan start, TimeSpan end)? cooldown, TimeSpan curTime)
        {
            _slots[slot].UpdateCooldown(cooldown, curTime);
        }

        /// <summary>
        /// Toggles the action on the indicated slot, showing it as toggled on or off based
        /// on toggleOn. Note that instant actions cannot be toggled.
        /// All others can, Target-based actions need to be toggleable so they can indicate
        /// which one you are currently picking a target for.
        /// </summary>
        /// <param name="slot">slot index containing the action to toggle
        /// (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        /// <param name="toggledOn">true to toggle it on, false to toggle it off</param>
        public void ToggleSlot(byte slot, bool toggledOn)
        {
            var actionSlot = _slots[slot];
            if (actionSlot.ToggleMode == false)
            {
                Logger.DebugS("action", "tried to toggle action slot for type {0} that is " +
                                        "not in toggle mode", actionSlot.Action?.ActionType);
                return;
            }
            if (actionSlot.Pressed == toggledOn) return;

            actionSlot.Pressed = toggledOn;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.Update(args);
            _dragDropHelper.Update(args.DeltaSeconds);
        }
    }

    /// <summary>
    /// Args for dragging and dropping the contents of one slot onto another slot.
    /// </summary>
    public class ActionSlotDragDropEventArgs : EventArgs
    {
        public readonly ActionSlot FromSlot;
        public readonly ActionSlot ToSlot;

        public ActionSlotDragDropEventArgs(ActionSlot fromSlot, ActionSlot toSlot)
        {
            FromSlot = fromSlot;
            ToSlot = toSlot;
        }
    }

    /// <summary>
    /// Args for interaction with an action slot
    /// </summary>
    public class ActionSlotEventArgs : EventArgs
    {
        /// <summary>
        /// Type of event
        /// </summary>
        public readonly ActionSlotEvent ActionSlotEvent;
        /// <summary>
        /// For Toggle events, whether the action is being toggled on or off.
        /// </summary>
        public readonly bool ToggleOn;
        /// <summary>
        /// Action slot being interacted with
        /// </summary>
        public readonly ActionSlot ActionSlot;
        public readonly BaseButton.ButtonEventArgs ButtonEventArgs;
        /// <summary>
        /// Action in the slot being interacted with
        /// </summary>
        public ActionPrototype Action => ActionSlot.Action;

        public ActionSlotEventArgs(ActionSlotEvent actionSlotEvent, bool toggleOn, ActionSlot actionSlot, BaseButton.ButtonEventArgs buttonEventArgs)
        {
            ActionSlotEvent = actionSlotEvent;
            ToggleOn = toggleOn;
            ActionSlot = actionSlot;
            ButtonEventArgs = buttonEventArgs;
        }
    }

    public enum ActionSlotEvent
    {
        /// <summary>
        /// Left clicking an instant action
        /// </summary>
        Press,
        /// <summary>
        /// left clicking a non-instant action (toggles it)
        /// </summary>
        Toggle,
        /// <summary>
        /// Right clicking an action
        /// </summary>
        RightClick
    }

}
