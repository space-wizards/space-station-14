using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Controls;
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
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
        private readonly Action<GUIMouseHoverEventArgs> _onMouseEnteredAction;
        private readonly Action<GUIMouseHoverEventArgs> _onMouseExitedAction;
        private readonly ActionSlot[] _slots;

        private readonly VBoxContainer _hotbarContainer;
        private readonly GridContainer _slotContainer;

        private readonly TextureButton _lockButton;
        private readonly TextureButton _settingsButton;
        private readonly TextureButton _previousHotbarButton;
        private readonly Label _loadoutNumber;
        private readonly TextureButton _nextHotbarButton;
        private readonly IClyde _clyde;
        private readonly Texture _lockTexture;
        private readonly Texture _unlockTexture;

        private readonly TextureRect _dragShadow;
        private readonly DragDropHelper<ActionSlot> _dragDropHelper;
        public bool IsDragging => _dragDropHelper.IsDragging;
        /// <summary>
        /// Whether the bar is currently locked by the user. This is intended to prevent drag / drop
        /// and right click clearing slots. Anything else is still doable.
        /// </summary>
        public bool Locked { get; private set; }

        /// <summary>
        /// All the action slots in order.
        /// </summary>
        public IEnumerable<ActionSlot> Slots => _slots;

        /// <param name="onShowTooltip">OnShowTooltip handler to assign to each ActionSlot</param>
        /// <param name="onHideTooltip">OnHideTooltip handler to assign to each ActionSlot</param>
        /// <param name="onPressAction">OnPressed handler to assign to each action slot</param>
        /// <param name="onDragDropAction">invoked when dragging and dropping an action from
        /// one slot to another.</param>
        /// <param name="onMouseEnteredAction">OnMouseEntered handler to assign to each action slot</param>
        /// <param name="onMouseExitedAction">OnMouseExited handler to assign to each action slot</param>
        /// <param name="onNextHotbarPressed">invoked when pressing the next hotbar button</param>
        /// <param name="onPreviousHotbarPressed">invoked when pressing the previous hotbar button</param>
        /// <param name="onSettingsButtonPressed">invoked when pressing the settings button</param>
        public ActionsUI(EventHandler onShowTooltip, EventHandler onHideTooltip, Action<BaseButton.ButtonEventArgs> onPressAction,
            Action<ActionSlotDragDropEventArgs> onDragDropAction, Action<GUIMouseHoverEventArgs> onMouseEnteredAction,
            Action<GUIMouseHoverEventArgs> onMouseExitedAction,
            Action<BaseButton.ButtonEventArgs> onNextHotbarPressed, Action<BaseButton.ButtonEventArgs> onPreviousHotbarPressed,
            Action<BaseButton.ButtonEventArgs> onSettingsButtonPressed)
        {
            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.End);
            LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopLeft, margin: 10);
            LayoutContainer.SetMarginTop(this, 100);

            _clyde = IoCManager.Resolve<IClyde>();
            _onShowTooltip = onShowTooltip;
            _onHideTooltip = onHideTooltip;
            _onPressAction = onPressAction;
            _onDragDropAction = onDragDropAction;
            _onMouseEnteredAction = onMouseEnteredAction;
            _onMouseExitedAction = onMouseExitedAction;
            _onNextHotbarPressed = onNextHotbarPressed;
            _onPreviousHotbarPressed = onPreviousHotbarPressed;
            _onSettingsButtonPressed = onSettingsButtonPressed;

            SizeFlagsHorizontal = SizeFlags.None;
            SizeFlagsVertical = SizeFlags.FillExpand;

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            _hotbarContainer = new VBoxContainer
            {
                SeparationOverride = 3,
                SizeFlagsHorizontal = SizeFlags.None
            };
            AddChild(_hotbarContainer);

            var settingsContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _hotbarContainer.AddChild(settingsContainer);

            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });
            _lockTexture = resourceCache.GetTexture("/Textures/Interface/Nano/lock.svg.png");
            _unlockTexture = resourceCache.GetTexture("/Textures/Interface/Nano/lock_open.svg.png");
            _lockButton = new TextureButton
            {
                TextureNormal = _unlockTexture,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            _lockButton.OnPressed += OnLockPressed;
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

            // this allows a 2 column layout if window gets too small
            _slotContainer = new GridContainer
            {
                MaxHeight = CalcMaxHeight(_clyde.ScreenSize)
            };
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
                var slot = new ActionSlot(i) {EnableAllKeybinds = true};
                slot.OnShowTooltip += onShowTooltip;
                slot.OnHideTooltip += onHideTooltip;
                slot.OnButtonDown += ActionSlotOnButtonDown;
                slot.OnButtonUp += ActionSlotOnButtonUp;
                slot.OnPressed += _onPressAction;
                slot.OnMouseEntered += _onMouseEnteredAction;
                slot.OnMouseExited += _onMouseExitedAction;
                _slotContainer.AddChild(slot);
                _slots[i - 1] = slot;
            }

            _dragDropHelper = new DragDropHelper<ActionSlot>(OnBeginActionDrag, OnContinueActionDrag, OnEndActionDrag);
            _clyde.OnWindowResized += ClydeOnOnWindowResized;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _nextHotbarButton.OnPressed -= _onNextHotbarPressed;
            _previousHotbarButton.OnPressed -= _onPreviousHotbarPressed;
            _settingsButton.OnPressed -= _onSettingsButtonPressed;
            _clyde.OnWindowResized -= ClydeOnOnWindowResized;
            foreach (var slot in _slots)
            {
                slot.OnShowTooltip -= _onShowTooltip;
                slot.OnHideTooltip -= _onHideTooltip;
                slot.OnPressed -= _onPressAction;
                slot.OnMouseEntered -= _onMouseEnteredAction;
                slot.OnMouseExited -= _onMouseExitedAction;
            }
        }

        private float CalcMaxHeight(Vector2i screenSize)
        {
            // it looks bad to have an uneven number of slots in the columns,
            // so we either do a single column or 2 equal sized columns
            if (((screenSize.Y) / UIScale) < 950)
            {
                // 2 column
                return 400;
            }
            else
            {
                // 1 column
                return 900;
            }
        }

        protected override void UIScaleChanged()
        {
            _slotContainer.MaxHeight = CalcMaxHeight(_clyde.ScreenSize);
            base.UIScaleChanged();
        }

        private void ClydeOnOnWindowResized(WindowResizedEventArgs obj)
        {
            // TODO: Can rework this once https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
            // this is here because there isn't currently a good way to allow the grid to adjust its height based
            // on constraints, otherwise we would use anchors to lay it out
            _slotContainer.MaxHeight = CalcMaxHeight(obj.NewSize);
        }

        private void OnLockPressed(BaseButton.ButtonEventArgs obj)
        {
            Locked = !Locked;
            _lockButton.TextureNormal = Locked ? _lockTexture : _unlockTexture;
        }


        private bool OnBeginActionDrag()
        {
            // only initiate the drag if the slot has an action in it
            if (Locked || _dragDropHelper.Dragged.Action == null) return false;

            _dragShadow.Texture = _dragDropHelper.Dragged.Action.Icon.Frame0();
            // don't make visible until frameupdate, otherwise it'll flicker
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled - (32, 32));
            return true;
        }

        private bool OnContinueActionDrag(float frameTime)
        {
            // stop if there's no action in the slot
            if (Locked || _dragDropHelper.Dragged.Action == null) return false;

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
            if (Locked || args.Event.Function != EngineKeyFunctions.Use) return;
            _dragDropHelper.MouseDown(args.Button as ActionSlot);
        }

        private void ActionSlotOnButtonUp(BaseButton.ButtonEventArgs args)
        {
            // note the buttonup only fires on the control that was originally
            // pressed to initiate the drag, NOT the one we are currently hovering
            if (Locked || args.Event.Function != EngineKeyFunctions.Use) return;

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
