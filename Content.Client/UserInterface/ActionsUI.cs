using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Controls;
using Content.Client.Utility;
using Content.Shared.Actions;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The action hotbar on the left side of the screen.
    /// </summary>
    public sealed class ActionsUI : PanelContainer
    {
        private readonly EventHandler _onShowTooltip;
        private readonly EventHandler _onHideTooltip;
        private readonly Action<BaseButton.ButtonEventArgs> _onPressAction;
        private readonly Action<ActionSlotDragDropEventArgs> _onDragDropAction;
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

        /// <summary>
        /// All the action slots in order.
        /// </summary>
        public IEnumerable<ActionSlot> Slots => _slots;

        /// <param name="onShowTooltip">OnShowTooltip handler to assign to each ActionSlot</param>
        /// <param name="onHideTooltip">OnHideTooltip handler to assign to each ActionSlot</param>
        /// <param name="onPressAction">OnPressed handler to assign to each action slot</param>
        /// <param name="onDragDropAction">invoked when dragging and dropping an action from
        /// one slot to another.</param>
        /// <param name="onNextHotbarPressed">invoked when pressing the next hotbar button</param>
        /// <param name="onPreviousHotbarPressed">invoked when pressing the previous hotbar button</param>
        /// <param name="onSettingsButtonPressed">invoked when pressing the settings button</param>
        public ActionsUI(EventHandler onShowTooltip, EventHandler onHideTooltip, Action<BaseButton.ButtonEventArgs> onPressAction,
            Action<ActionSlotDragDropEventArgs> onDragDropAction,
            Action<BaseButton.ButtonEventArgs> onNextHotbarPressed, Action<BaseButton.ButtonEventArgs> onPreviousHotbarPressed,
            Action<BaseButton.ButtonEventArgs> onSettingsButtonPressed)
        {
            _onShowTooltip = onShowTooltip;
            _onHideTooltip = onHideTooltip;
            _onPressAction = onPressAction;
            _onDragDropAction = onDragDropAction;
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
                slot.OnPressed += _onPressAction;
                _slotContainer.AddChild(slot);
                _slots[i - 1] = slot;
            }

            _dragDropHelper = new DragDropHelper<ActionSlot>(OnBeginActionDrag, OnContinueActionDrag, OnEndActionDrag);
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
                slot.OnPressed -= _onPressAction;
            }
        }

        private bool OnBeginActionDrag()
        {
            // only initiate the drag if the slot has an action in it
            if (_dragDropHelper.Dragged.Action == null) return false;

            _dragShadow.Texture = _dragDropHelper.Dragged.Action.Icon.Frame0();
            // don't make visible until frameupdate, otherwise it'll flicker
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled - (32, 32));
            return true;
        }

        private bool OnContinueActionDrag(float frameTime)
        {
            // stop if there's no action in the slot
            if (_dragDropHelper.Dragged.Action == null) return false;

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
                if (!_dragDropHelper.IsDragging || _dragDropHelper.Dragged?.Action == null)
                {
                    _dragDropHelper.EndDrag();
                    return;
                }

                // drag and drop
                _onDragDropAction?.Invoke(new ActionSlotDragDropEventArgs(_dragDropHelper.Dragged, targetSlot));
            }

            _dragDropHelper.EndDrag();
        }

        /// <summary>
        /// Handle keydown / keyup for one of the slots via a keybinding, simulates mousedown/mouseup on it.
        /// </summary>
        /// <param name="slot">slot index to to receive the press (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        public void HandleHotbarKeybind(byte slot, PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            var actionSlot = _slots[slot];
            actionSlot.HandleKeybind(args.State);
        }

        public void SetHotbarLabel(int number)
        {
            _loadoutNumber.Text = number.ToString();
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
}
