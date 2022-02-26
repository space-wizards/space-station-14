using Content.Client.DragDrop;
using Content.Client.HUD;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Actions.UI
{
    /// <summary>
    ///     The action hotbar on the left side of the screen.
    /// </summary>
    public sealed class ActionsUI : Container
    {
        private const float DragDeadZone = 10f;
        private const float CustomTooltipDelay = 0.4f;
        internal readonly ActionsSystem System;
        private readonly IGameHud _gameHud;

        /// <summary>
        ///     The action component of the currently attached entity.
        /// </summary>
        public readonly ActionsComponent Component;

        private readonly ActionSlot[] _slots;

        private readonly GridContainer _slotContainer;

        private readonly TextureButton _lockButton;
        private readonly TextureButton _settingsButton;
        private readonly Label _loadoutNumber;
        private readonly Texture _lockTexture;
        private readonly Texture _unlockTexture;
        private readonly BoxContainer _loadoutContainer;

        private readonly TextureRect _dragShadow;

        private readonly ActionMenu _menu;

        /// <summary>
        /// Index of currently selected hotbar
        /// </summary>
        public byte SelectedHotbar { get; private set; }

        /// <summary>
        /// Action slot we are currently selecting a target for.
        /// </summary>
        public ActionSlot? SelectingTargetFor { get; private set; }

        /// <summary>
        /// Drag drop helper for coordinating drag drops between action slots
        /// </summary>
        public DragDropHelper<ActionSlot> DragDropHelper { get; }

        /// <summary>
        /// Whether the bar is currently locked by the user. This is intended to prevent drag / drop
        /// and right click clearing slots. Anything else is still doable.
        /// </summary>
        public bool Locked { get; private set; }

        /// <summary>
        /// All the action slots in order.
        /// </summary>
        public IEnumerable<ActionSlot> Slots => _slots;

        public ActionsUI(ActionsSystem system, ActionsComponent component)
        {
            SetValue(LayoutContainer.DebugProperty, true);
            System = system;
            Component = component;
            _gameHud = IoCManager.Resolve<IGameHud>();
            _menu = new ActionMenu(this);

            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.End);
            LayoutContainer.SetGrowVertical(this, LayoutContainer.GrowDirection.Constrain);
            LayoutContainer.SetAnchorTop(this, 0f);
            LayoutContainer.SetAnchorBottom(this, 0.8f);
            LayoutContainer.SetMarginLeft(this, 13);
            LayoutContainer.SetMarginTop(this, 110);

            HorizontalAlignment = HAlignment.Left;
            VerticalExpand = true;

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            // everything needs to go within an inner panel container so the panel resizes to fit the elements.
            // Because ActionsUI is being anchored by layoutcontainer, the hotbar backing would appear too tall
            // if ActionsUI was the panel container

            var panelContainer = new PanelContainer()
            {
                StyleClasses = {StyleNano.StyleClassHotbarPanel},
                HorizontalAlignment = HAlignment.Left,
                VerticalAlignment = VAlignment.Top
            };
            AddChild(panelContainer);

            var hotbarContainer = new BoxContainer
            {
	            Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 3,
                HorizontalAlignment = HAlignment.Left
            };
            panelContainer.AddChild(hotbarContainer);

            var settingsContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };
            hotbarContainer.AddChild(settingsContainer);

            settingsContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 1 });
            _lockTexture = resourceCache.GetTexture("/Textures/Interface/Nano/lock.svg.192dpi.png");
            _unlockTexture = resourceCache.GetTexture("/Textures/Interface/Nano/lock_open.svg.192dpi.png");
            _lockButton = new TextureButton
            {
                TextureNormal = _unlockTexture,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1,
                Scale = (0.5f, 0.5f),
                ToolTip = Loc.GetString("ui-actionsui-function-lock-action-slots"),
                TooltipDelay = CustomTooltipDelay
            };
            settingsContainer.AddChild(_lockButton);
            settingsContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 2 });
            _settingsButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/gear.svg.192dpi.png"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1,
                Scale = (0.5f, 0.5f),
                ToolTip = Loc.GetString("ui-actionsui-function-open-abilities-menu"),
                TooltipDelay = CustomTooltipDelay
            };
            settingsContainer.AddChild(_settingsButton);
            settingsContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 1 });

            // this allows a 2 column layout if window gets too small
            _slotContainer = new GridContainer
            {
                MaxGridHeight = CalcMaxHeight()
            };
            hotbarContainer.AddChild(_slotContainer);

            _loadoutContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                MouseFilter = MouseFilterMode.Stop
            };
            hotbarContainer.AddChild(_loadoutContainer);

            _loadoutContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 1 });
            var previousHotbarIcon = new TextureRect()
            {
                Texture = resourceCache.GetTexture("/Textures/Interface/Nano/left_arrow.svg.192dpi.png"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1,
                TextureScale = (0.5f, 0.5f)
            };
            _loadoutContainer.AddChild(previousHotbarIcon);
            _loadoutContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 2 });
            _loadoutNumber = new Label
            {
                Text = "1",
                SizeFlagsStretchRatio = 1
            };
            _loadoutContainer.AddChild(_loadoutNumber);
            _loadoutContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 2 });
            var nextHotbarIcon = new TextureRect
            {
                Texture = resourceCache.GetTexture("/Textures/Interface/Nano/right_arrow.svg.192dpi.png"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1,
                TextureScale = (0.5f, 0.5f)
            };
            _loadoutContainer.AddChild(nextHotbarIcon);
            _loadoutContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 1 });

            _slots = new ActionSlot[ActionsSystem.Slots];

            _dragShadow = new TextureRect
            {
                MinSize = (64, 64),
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false,
                SetSize = (64, 64)
            };
            UserInterfaceManager.PopupRoot.AddChild(_dragShadow);

            for (byte i = 0; i < ActionsSystem.Slots; i++)
            {
                var slot = new ActionSlot(this, _menu, i);
                _slotContainer.AddChild(slot);
                _slots[i] = slot;
            }

            DragDropHelper = new DragDropHelper<ActionSlot>(OnBeginActionDrag, OnContinueActionDrag, OnEndActionDrag, DragDeadZone);

            MinSize = (10, 400);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _lockButton.OnPressed += OnLockPressed;
            _settingsButton.OnPressed += OnToggleActionsMenu;
            _loadoutContainer.OnKeyBindDown += OnHotbarPaginate;
            _gameHud.ActionsButtonToggled += OnToggleActionsMenuTopButton;
            _gameHud.ActionsButtonDown = false;
            _gameHud.ActionsButtonVisible = true;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            StopTargeting();
            _menu.Close();
            _lockButton.OnPressed -= OnLockPressed;
            _settingsButton.OnPressed -= OnToggleActionsMenu;
            _loadoutContainer.OnKeyBindDown -= OnHotbarPaginate;
            _gameHud.ActionsButtonToggled -= OnToggleActionsMenuTopButton;
            _gameHud.ActionsButtonDown = false;
            _gameHud.ActionsButtonVisible = false;
        }

        protected override void Resized()
        {
            base.Resized();
            _slotContainer.MaxGridHeight = CalcMaxHeight();
        }

        private float CalcMaxHeight()
        {
            // TODO: Can rework this once https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
            // this is here because there isn't currently a good way to allow the grid to adjust its height based
            // on constraints, otherwise we would use anchors to lay it out

            // it looks bad to have an uneven number of slots in the columns,
            // so we either do a single column or 2 equal sized columns
            if (Height < 650)
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
            _slotContainer.MaxGridHeight = CalcMaxHeight();
            base.UIScaleChanged();
        }

        /// <summary>
        /// Refresh the display of all the slots in the currently displayed hotbar,
        /// to reflect the current component state and assignments of actions component.
        /// </summary>
        public void UpdateUI()
        {
            _menu.UpdateUI();

            foreach (var actionSlot in Slots)
            {
                var action = System.Assignments[SelectedHotbar, actionSlot.SlotIndex];

                if (action == null)
                {
                    if (SelectingTargetFor == actionSlot)
                        StopTargeting(true);
                    actionSlot.Clear();
                    continue;
                }

                if (Component.Actions.TryGetValue(action, out var actualAction))
                {
                    UpdateActionSlot(actualAction, actionSlot);
                    continue;
                }

                // Action not in the actions component, but in the assignment list.
                // This is either an action that doesn't auto-clear from the menu, or the action menu was locked.
                // Show the old action, but make sure it is disabled;
                action.Enabled = false;
                action.Toggled = false;

                // If we enable the item-sprite, and if the item-sprite has a visual toggle, then the player will be
                // able to know whether the item is toggled, even if it is not in their LOS (but in PVS). And for things
                // like PDA sprites, the player can even see whether the action's item is currently inside of their PVS.
                // SO unless theres some way of "freezing" a sprite-view, we just have to disable it.
                action.ItemIconStyle = ItemActionIconStyle.NoItem;

                UpdateActionSlot(action, actionSlot);
            }
        }

        private void UpdateActionSlot(ActionType action, ActionSlot actionSlot)
        {
            actionSlot.Assign(action);

            if (!action.Enabled)
            {
                // just revoked an action we were trying to target with, stop targeting
                if (SelectingTargetFor?.Action != null && SelectingTargetFor.Action == action)
                {
                    StopTargeting();
                }

                actionSlot.Disable();
            }
            else
            {
                actionSlot.Enable();
            }

            actionSlot.UpdateIcons();
            actionSlot.DrawModeChanged();
        }

        private void OnHotbarPaginate(GUIBoundKeyEventArgs args)
        {
            // rather than clicking the arrows themselves, the user can click the hbox so it's more
            // "forgiving" for misclicks, and we simply check which side they are closer to
            if (args.Function != EngineKeyFunctions.UIClick) return;

            var rightness = args.RelativePosition.X / _loadoutContainer.Width;
            if (rightness > 0.5)
            {
                ChangeHotbar((byte) ((SelectedHotbar + 1) % ActionsSystem.Hotbars));
            }
            else
            {
                var newBar = SelectedHotbar == 0 ? ActionsSystem.Hotbars - 1 : SelectedHotbar - 1;
                ChangeHotbar((byte) newBar);
            }
        }

        private void ChangeHotbar(byte hotbar)
        {
            StopTargeting();
            SelectedHotbar = hotbar;
            _loadoutNumber.Text = (hotbar + 1).ToString();
            UpdateUI();
        }

        /// <summary>
        /// If currently targeting with this slot, stops targeting.
        /// If currently targeting with no slot or a different slot, switches to
        /// targeting with the specified slot.
        /// </summary>
        /// <param name="slot"></param>
        public void ToggleTargeting(ActionSlot slot)
        {
            if (SelectingTargetFor == slot)
            {
                StopTargeting();
                return;
            }
            StartTargeting(slot);
        }

        /// <summary>
        /// Puts us in targeting mode, where we need to pick either a target point or entity
        /// </summary>
        private void StartTargeting(ActionSlot actionSlot)
        {
            if (actionSlot.Action == null)
                return;

            // If we were targeting something else we should stop
            StopTargeting();

            SelectingTargetFor = actionSlot;

            if (actionSlot.Action is TargetedAction targetAction)
                System.StartTargeting(targetAction);

            UpdateUI();
        }

        /// <summary>
        /// Switch out of targeting mode if currently selecting target for an action
        /// </summary>
        public void StopTargeting(bool updating = false)
        {
            if (SelectingTargetFor == null)
                return;

            SelectingTargetFor = null;
            System.StopTargeting();

            // Sometimes targeting gets stopped mid-UI update.
            // in that case, don't need to do a nested UI refresh.
            if (!updating)
                UpdateUI();
        }

        private void OnToggleActionsMenu(BaseButton.ButtonEventArgs args)
        {
            ToggleActionsMenu();
        }

        private void OnToggleActionsMenuTopButton(bool open)
        {
            if (open == _menu.IsOpen) return;
            ToggleActionsMenu();
        }

        public void ToggleActionsMenu()
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

        private void OnLockPressed(BaseButton.ButtonEventArgs obj)
        {
            Locked = !Locked;
            _lockButton.TextureNormal = Locked ? _lockTexture : _unlockTexture;
        }

        private bool OnBeginActionDrag()
        {
            // only initiate the drag if the slot has an action in it
            if (Locked || DragDropHelper.Dragged?.Action == null) return false;

            _dragShadow.Texture = DragDropHelper.Dragged.Action.Icon?.Frame0();
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled.Position - (32, 32));
            DragDropHelper.Dragged.CancelPress();
            return true;
        }

        private bool OnContinueActionDrag(float frameTime)
        {
            // stop if there's no action in the slot
            if (Locked || DragDropHelper.Dragged?.Action == null) return false;

            // keep dragged entity centered under mouse
            LayoutContainer.SetPosition(_dragShadow, UserInterfaceManager.MousePositionScaled.Position - (32, 32));
            // we don't set this visible until frameupdate, otherwise it flickers
            _dragShadow.Visible = true;
            return true;
        }

        private void OnEndActionDrag()
        {
            _dragShadow.Visible = false;
        }

        /// <summary>
        /// Handle keydown / keyup for one of the slots via a keybinding, simulates mousedown/mouseup on it.
        /// </summary>
        /// <param name="slot">slot index to to receive the press (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        public void HandleHotbarKeybind(byte slot, PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            var actionSlot = _slots[slot];
            actionSlot.Depress(args.State == BoundKeyState.Down);
            actionSlot.DrawModeChanged();
        }

        /// <summary>
        /// Handle hotbar change.
        /// </summary>
        /// <param name="hotbar">hotbar index to switch to</param>
        public void HandleChangeHotbarKeybind(byte hotbar, PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            ChangeHotbar(hotbar);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            DragDropHelper.Update(args.DeltaSeconds);
        }
    }
}
