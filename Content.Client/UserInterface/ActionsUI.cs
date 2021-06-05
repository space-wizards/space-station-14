using System.Collections.Generic;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.GameObjects.Components.Mobs.Actions;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Actions;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
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
    public sealed class ActionsUI : Container
    {
        private readonly ClientActionsComponent _actionsComponent;
        private readonly ActionManager _actionManager;
        private readonly IEntityManager _entityManager;
        private readonly IGameTiming _gameTiming;
        private readonly IGameHud _gameHud;

        private readonly ActionSlot[] _slots;

        private readonly GridContainer _slotContainer;

        private readonly TextureButton _lockButton;
        private readonly TextureButton _settingsButton;
        private readonly Label _loadoutNumber;
        private readonly Texture _lockTexture;
        private readonly Texture _unlockTexture;
        private readonly HBoxContainer _loadoutContainer;

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

        public ActionsUI(ClientActionsComponent actionsComponent)
        {
            SetValue(LayoutContainer.DebugProperty, true);
            _actionsComponent = actionsComponent;
            _actionManager = IoCManager.Resolve<ActionManager>();
            _entityManager = IoCManager.Resolve<IEntityManager>();
            _gameTiming = IoCManager.Resolve<IGameTiming>();
            _gameHud = IoCManager.Resolve<IGameHud>();
            _menu = new ActionMenu(_actionsComponent, this);

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

            var hotbarContainer = new VBoxContainer
            {
                SeparationOverride = 3,
                HorizontalAlignment = HAlignment.Left
            };
            panelContainer.AddChild(hotbarContainer);

            var settingsContainer = new HBoxContainer
            {
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
                Scale = (0.5f, 0.5f)
            };
            settingsContainer.AddChild(_lockButton);
            settingsContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 2 });
            _settingsButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/gear.svg.192dpi.png"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                SizeFlagsStretchRatio = 1,
                Scale = (0.5f, 0.5f)
            };
            settingsContainer.AddChild(_settingsButton);
            settingsContainer.AddChild(new Control { HorizontalExpand = true, SizeFlagsStretchRatio = 1 });

            // this allows a 2 column layout if window gets too small
            _slotContainer = new GridContainer
            {
                MaxGridHeight = CalcMaxHeight()
            };
            hotbarContainer.AddChild(_slotContainer);

            _loadoutContainer = new HBoxContainer
            {
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

            _slots = new ActionSlot[ClientActionsComponent.Slots];

            _dragShadow = new TextureRect
            {
                MinSize = (64, 64),
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false,
                SetSize = (64, 64)
            };
            UserInterfaceManager.PopupRoot.AddChild(_dragShadow);

            for (byte i = 0; i < ClientActionsComponent.Slots; i++)
            {
                var slot = new ActionSlot(this, _menu, actionsComponent, i);
                _slotContainer.AddChild(slot);
                _slots[i] = slot;
            }

            DragDropHelper = new DragDropHelper<ActionSlot>(OnBeginActionDrag, OnContinueActionDrag, OnEndActionDrag);

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
                var assignedActionType = _actionsComponent.Assignments[SelectedHotbar, actionSlot.SlotIndex];
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
                    Logger.ErrorS("action", "unexpected Assignment type {0}",
                        assignedActionType.Value.Assignment);
                    actionSlot.Clear();
                }
            }
        }

        private void UpdateActionSlot(ActionType actionType, ActionSlot actionSlot, ActionAssignment? assignedActionType)
        {
            if (_actionManager.TryGet(actionType, out var action))
            {
                actionSlot.Assign(action, true);
            }
            else
            {
                Logger.ErrorS("action", "unrecognized actionType {0}", assignedActionType);
                actionSlot.Clear();
                return;
            }

            if (!_actionsComponent.TryGetActionState(actionType, out var actionState) || !actionState.Enabled)
            {
                // action is currently disabled

                // just revoked an action we were trying to target with, stop targeting
                if (SelectingTargetFor?.Action != null && SelectingTargetFor.Action == action)
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

                // if we are targeting for this action and it's now on cooldown, stop targeting if we're supposed to
                if (SelectingTargetFor?.Action != null && SelectingTargetFor.Action == action &&
                    actionState.IsOnCooldown(_gameTiming) && action.DeselectOnCooldown)
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
            if (_actionManager.TryGet(itemlessActionType, out var action))
            {
                actionSlot.Assign(action);
            }
            else
            {
                Logger.ErrorS("action", "unrecognized actionType {0}", assignedActionType);
                actionSlot.Clear();
            }
            actionSlot.Cooldown = null;
        }

        private void UpdateActionSlot(EntityUid item, ItemActionType itemActionType, ActionSlot actionSlot,
            ActionAssignment? assignedActionType)
        {
            if (!_entityManager.TryGetEntity(item, out var itemEntity)) return;
            if (_actionManager.TryGet(itemActionType, out var action))
            {
                actionSlot.Assign(action, itemEntity, true);
            }
            else
            {
                Logger.ErrorS("action", "unrecognized actionType {0}", assignedActionType);
                actionSlot.Clear();
                return;
            }

            if (!_actionsComponent.TryGetItemActionState(itemActionType, item, out var actionState))
            {
                // action is no longer tied to an item, this should never happen as we
                // check this at the start of this method. But just to be safe
                // we will restore our assignment here to the correct state
                Logger.ErrorS("action", "coding error, expected actionType {0} to have" +
                                          " a state but it didn't", assignedActionType);
                _actionsComponent.Assignments.AssignSlot(SelectedHotbar, actionSlot.SlotIndex,
                    ActionAssignment.For(itemActionType));
                actionSlot.Assign(action);
                return;
            }

            if (!actionState.Enabled)
            {
                // just disabled an action we were trying to target with, stop targeting
                if (SelectingTargetFor?.Action != null && SelectingTargetFor.Action == action)
                {
                    StopTargeting();
                }

                actionSlot.DisableAction();
            }
            else
            {
                // action is currently granted
                actionSlot.EnableAction();

                // if we are targeting with an action now on cooldown, stop targeting if we should
                if (SelectingTargetFor?.Action != null && SelectingTargetFor.Action == action &&
                    SelectingTargetFor.Item == itemEntity &&
                    actionState.IsOnCooldown(_gameTiming) && action.DeselectOnCooldown)
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


        private void OnHotbarPaginate(GUIBoundKeyEventArgs args)
        {
            // rather than clicking the arrows themselves, the user can click the hbox so it's more
            // "forgiving" for misclicks, and we simply check which side they are closer to
            if (args.Function != EngineKeyFunctions.UIClick) return;

            var rightness = args.RelativePosition.X / _loadoutContainer.Width;
            if (rightness > 0.5)
            {
                ChangeHotbar((byte) ((SelectedHotbar + 1) % ClientActionsComponent.Hotbars));
            }
            else
            {
                var newBar = SelectedHotbar == 0 ? ClientActionsComponent.Hotbars - 1 : SelectedHotbar - 1;
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
            // If we were targeting something else we should stop
            StopTargeting();

            SelectingTargetFor = actionSlot;

            // show it as toggled on to indicate we are currently selecting a target for it
            if (!actionSlot.ToggledOn)
            {
                actionSlot.ToggledOn = true;
            }
        }

        /// <summary>
        /// Switch out of targeting mode if currently selecting target for an action
        /// </summary>
        public void StopTargeting()
        {
            if (SelectingTargetFor == null) return;
            if (SelectingTargetFor.ToggledOn)
            {
                SelectingTargetFor.ToggledOn = false;
            }
            SelectingTargetFor = null;
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

            _dragShadow.Texture = DragDropHelper.Dragged.Action.Icon.Frame0();
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
