using System;
using Content.Client.Cooldown;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Actions.UI
{
    /// <summary>
    /// A slot in the action hotbar. Not extending BaseButton because
    /// its needs diverged too much.
    /// </summary>
    public sealed class ActionSlot : PanelContainer
    {
        // shorter than default tooltip delay so user can more easily
        // see what actions they've been given
        private const float CustomTooltipDelay = 0.5f;

        private static readonly string EnabledColor = "#7b7e9e";
        private static readonly string DisabledColor = "#950000";

        /// <summary>
        /// Current action in this slot.
        /// </summary>
        public ActionType? Action { get; private set; }

        /// <summary>
        /// 1-10 corresponding to the number label on the slot (10 is labeled as 0)
        /// </summary>
        private byte SlotNumber => (byte) (SlotIndex + 1);
        public byte SlotIndex { get; }

        private readonly IGameTiming _gameTiming;
        private readonly RichTextLabel _number;
        private readonly TextureRect _bigActionIcon;
        private readonly TextureRect _smallActionIcon;
        private readonly SpriteView _smallItemSpriteView;
        private readonly SpriteView _bigItemSpriteView;
        private readonly CooldownGraphic _cooldownGraphic;
        private readonly ActionsUI _actionsUI;
        private readonly ActionMenu _actionMenu;
        // whether button is currently pressed down by mouse or keybind down.
        private bool _depressed;
        private bool _beingHovered;

        /// <summary>
        /// Creates an action slot for the specified number
        /// </summary>
        /// <param name="slotIndex">slot index this corresponds to, 0-9 (0 labeled as 1, 8, labeled "9", 9 labeled as "0".</param>
        public ActionSlot(ActionsUI actionsUI, ActionMenu actionMenu, byte slotIndex)
        {
            _actionsUI = actionsUI;
            _actionMenu = actionMenu;
            _gameTiming = IoCManager.Resolve<IGameTiming>();
            SlotIndex = slotIndex;
            MouseFilter = MouseFilterMode.Stop;

            MinSize = (64, 64);
            VerticalAlignment = VAlignment.Top;
            TooltipDelay = CustomTooltipDelay;
            TooltipSupplier = SupplyTooltip;

            _number = new RichTextLabel
            {
                StyleClasses = {StyleNano.StyleClassHotbarSlotNumber}
            };
            _number.SetMessage(SlotNumberLabel());

            _bigActionIcon = new TextureRect
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false
            };
            _bigItemSpriteView = new SpriteView
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                Scale = (2,2),
                Visible = false,
                OverrideDirection = Direction.South,
            };
            _smallActionIcon = new TextureRect
            {
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom,
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false
            };
            _smallItemSpriteView = new SpriteView
            {
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom,
                Visible = false,
                OverrideDirection = Direction.South,
            };

            _cooldownGraphic = new CooldownGraphic {Progress = 0, Visible = false};

            // padding to the left of the number to shift it right
            var paddingBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
                MinSize = (64, 64)
            };
            paddingBox.AddChild(new Control()
            {
                MinSize = (4, 4),
            });
            paddingBox.AddChild(_number);

            // padding to the left of the small icon
            var paddingBoxItemIcon = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
                MinSize = (64, 64)
            };
            paddingBoxItemIcon.AddChild(new Control()
            {
                MinSize = (32, 32),
            });
            paddingBoxItemIcon.AddChild(new Control
            {
                Children =
                {
                    _smallActionIcon,
                    _smallItemSpriteView
                }
            });
            AddChild(_bigActionIcon);
            AddChild(_bigItemSpriteView);
            AddChild(_cooldownGraphic);
            AddChild(paddingBox);
            AddChild(paddingBoxItemIcon);
            DrawModeChanged();
        }

        private Control? SupplyTooltip(Control sender)
        {
            if (Action == null)
                return null;

            string? extra = null;
            if (Action.Charges != null)
            {
                extra = Loc.GetString("ui-actionslot-charges", ("charges", Action.Charges));
            }

            var name = FormattedMessage.FromMarkupPermissive(Loc.GetString(Action.Name));
            var decr = FormattedMessage.FromMarkupPermissive(Loc.GetString(Action.Description));

            var tooltip = new ActionAlertTooltip(name, decr, extra);

            if (Action.Enabled && (Action.Charges == null || Action.Charges != 0))
                tooltip.Cooldown = Action.Cooldown;

            return tooltip;
        }

        protected override void MouseEntered()
        {
            base.MouseEntered();

            _beingHovered = true;
            DrawModeChanged();

            if (Action?.Provider != null)
                _actionsUI.System.HighlightItemSlot(Action.Provider.Value);
        }

        protected override void MouseExited()
        {
            base.MouseExited();
            _beingHovered = false;
            CancelPress();
            DrawModeChanged();
            _actionsUI.System.StopHighlightingItemSlot();
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (Action == null)
            {
                // No action for this slot. Maybe the user is trying to add a mapping action?
                _actionsUI.System.TryFillSlot(_actionsUI.SelectedHotbar, SlotIndex);
                return;
            }

            // only handle clicks, and can't do anything to this if no assignment
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                // might turn into a drag or a full press if released
                Depress(true);
                _actionsUI.DragDropHelper.MouseDown(this);
                DrawModeChanged();
                return;
            }

            if (args.Function != EngineKeyFunctions.UIRightClick || _actionsUI.Locked)
                return;

            if (_actionsUI.DragDropHelper.IsDragging || _actionMenu.IsDragging)
                return;

            // user right clicked on an action slot, so we clear it.
            _actionsUI.System.Assignments.ClearSlot(_actionsUI.SelectedHotbar, SlotIndex, true);

            // If this was a temporary action, and it is no longer assigned to any slots, then we remove the action
            // altogether.
            if (Action.Temporary)
            {
                // Theres probably a better way to do this.....
                DebugTools.Assert(Action.ClientExclusive, "Temporary-actions must be client exclusive");

                if (!_actionsUI.System.Assignments.Assignments.TryGetValue(Action, out var index)
                    || index.Count == 0)
                {
                    _actionsUI.Component.Actions.Remove(Action);
                }
            }        

            _actionsUI.StopTargeting();
            _actionsUI.UpdateUI();
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            // might be finishing a drag or using the action
            if (_actionsUI.DragDropHelper.IsDragging &&
                _actionsUI.DragDropHelper.Dragged == this &&
                UserInterfaceManager.CurrentlyHovered is ActionSlot targetSlot &&
                targetSlot != this)
            {
                // finish the drag, swap the 2 slots
                var fromIdx = SlotIndex;
                var fromAssignment = _actionsUI.System.Assignments[_actionsUI.SelectedHotbar, fromIdx];
                var toIdx = targetSlot.SlotIndex;
                var toAssignment = _actionsUI.System.Assignments[_actionsUI.SelectedHotbar, toIdx];

                if (fromIdx == toIdx) return;
                if (fromAssignment == null) return;

                _actionsUI.System.Assignments.AssignSlot(_actionsUI.SelectedHotbar, toIdx, fromAssignment);
                if (toAssignment != null)
                {
                    _actionsUI.System.Assignments.AssignSlot(_actionsUI.SelectedHotbar, fromIdx, toAssignment);
                }
                else
                {
                    _actionsUI.System.Assignments.ClearSlot(_actionsUI.SelectedHotbar, fromIdx, false);
                }
                _actionsUI.UpdateUI();
            }
            else
            {
                // perform the action
                if (UserInterfaceManager.CurrentlyHovered == this)
                {
                    Depress(false);
                }
            }
            _actionsUI.DragDropHelper.EndDrag();
            DrawModeChanged();
        }

        protected override void ControlFocusExited()
        {
            // lost focus for some reason, cancel the drag if there is one.
            base.ControlFocusExited();
            _actionsUI.DragDropHelper.EndDrag();
            DrawModeChanged();
        }

        /// <summary>
        /// Cancel current press without triggering the action
        /// </summary>
        public void CancelPress()
        {
            _depressed = false;
            DrawModeChanged();
        }

        /// <summary>
        /// Press this button down. If it was depressed and now set to not depressed, will
        /// trigger the action.
        /// </summary>
        public void Depress(bool depress)
        {
            // action can still be toggled if it's allowed to stay selected
            if (Action == null || !Action.Enabled) return;

            if (_depressed && !depress)
            {
                // fire the action
                _actionsUI.System.OnSlotPressed(this);
            }
            _depressed = depress;
        }

        /// <summary>
        /// Updates the item action assigned to this slot, tied to a specific item.
        /// </summary>
        /// <param name="action">action to assign</param>
        /// <param name="item">item the action is provided by</param>
        public void Assign(ActionType action)
        {
            // already assigned
            if (Action != null && Action == action) return;

            Action = action;
            HideTooltip();
            UpdateIcons();
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Clears the action assigned to this slot
        /// </summary>
        public void Clear()
        {
            if (Action == null) return;
            Action = null;
            _depressed = false;
            HideTooltip();
            UpdateIcons();
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as enabled
        /// </summary>
        public void Enable()
        {
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as disabled.
        /// The slot is still clickable.
        /// </summary>
        public void Disable()
        {
            _depressed = false;
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        private FormattedMessage SlotNumberLabel()
        {
            if (SlotNumber > 10) return FormattedMessage.FromMarkup("");
            var number = Loc.GetString(SlotNumber == 10 ? "0" : SlotNumber.ToString());
            var color = (Action == null || Action.Enabled) ? EnabledColor : DisabledColor;
            return FormattedMessage.FromMarkup("[color=" + color + "]" + number + "[/color]");
        }

        public void UpdateIcons()
        {
            UpdateItemIcon();

            if (Action == null)
            {
                SetActionIcon(null);
                return;
            }

            if ((_actionsUI.SelectingTargetFor?.Action == Action || Action.Toggled) && Action.IconOn != null)
                SetActionIcon(Action.IconOn.Frame0());
            else
                SetActionIcon(Action.Icon?.Frame0());
        }

        private void SetActionIcon(Texture? texture)
        {
            if (texture == null || Action == null)
            {
                _bigActionIcon.Texture = null;
                _bigActionIcon.Visible = false;
                _smallActionIcon.Texture = null;
                _smallActionIcon.Visible = false;
            }
            else if (Action.Provider != null && Action.ItemIconStyle == ItemActionIconStyle.BigItem)
            {
                _smallActionIcon.Texture = texture;
                _smallActionIcon.Modulate = Action.IconColor;
                _smallActionIcon.Visible = true;
                _bigActionIcon.Texture = null;
                _bigActionIcon.Visible = false;
            }
            else
            {
                _bigActionIcon.Texture = texture;
                _bigActionIcon.Modulate = Action.IconColor;
                _bigActionIcon.Visible = true;
                _smallActionIcon.Texture = null;
                _smallActionIcon.Visible = false;
            }
        }

        private void UpdateItemIcon()
        {
            if (Action?.Provider == null || !IoCManager.Resolve<IEntityManager>().TryGetComponent(Action.Provider.Value, out SpriteComponent sprite))
            {
                _bigItemSpriteView.Visible = false;
                _bigItemSpriteView.Sprite = null;
                _smallItemSpriteView.Visible = false;
                _smallItemSpriteView.Sprite = null;
            }
            else
            {
                switch (Action.ItemIconStyle)
                {
                    case ItemActionIconStyle.BigItem:
                        _bigItemSpriteView.Visible = true;
                        _bigItemSpriteView.Sprite = sprite;
                        _smallItemSpriteView.Visible = false;
                        _smallItemSpriteView.Sprite = null;
                        break;
                    case ItemActionIconStyle.BigAction:

                        _bigItemSpriteView.Visible = false;
                        _bigItemSpriteView.Sprite = null;
                        _smallItemSpriteView.Visible = true;
                        _smallItemSpriteView.Sprite = sprite;
                        break;

                    case ItemActionIconStyle.NoItem:

                        _bigItemSpriteView.Visible = false;
                        _bigItemSpriteView.Sprite = null;
                        _smallItemSpriteView.Visible = false;
                        _smallItemSpriteView.Sprite = null;
                        break;
                }
            }
        }

        public void DrawModeChanged()
        {
            // always show the normal empty button style if no action in this slot
            if (Action == null)
            {
                SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
                return;
            }

            // show a hover only if the action is usable or another action is being dragged on top of this
            if (_beingHovered && (_actionsUI.DragDropHelper.IsDragging || _actionMenu.IsDragging || Action.Enabled))
            {
                SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassHover);
            }

            // it's only depress-able if it's usable, so if we're depressed
            // show the depressed style
            if (_depressed)
            {
                SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassPressed);
                return;
            }

            // if it's toggled on, always show the toggled on style (currently same as depressed style)
            if (Action.Toggled || _actionsUI.SelectingTargetFor == this)
            {
                // when there's a toggle sprite, we're showing that sprite instead of highlighting this slot
                SetOnlyStylePseudoClass(Action.IconOn != null ? ContainerButton.StylePseudoClassNormal :
                    ContainerButton.StylePseudoClassPressed);
                return;
            }

            if (!Action.Enabled)
            {
                SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassDisabled);
                return;
            }

            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (Action == null || Action.Cooldown == null || !Action.Enabled)
            {
                _cooldownGraphic.Visible = false;
                _cooldownGraphic.Progress = 0;
                return;
            }

            var cooldown = Action.Cooldown.Value;
            var duration = cooldown.End - cooldown.Start;
            var curTime = _gameTiming.CurTime;
            var length = duration.TotalSeconds;
            var progress = (curTime - cooldown.Start).TotalSeconds / length;
            var ratio = (progress <= 1 ? (1 - progress) : (curTime - cooldown.End).TotalSeconds * -5);

            _cooldownGraphic.Progress = MathHelper.Clamp((float)ratio, -1, 1);
            if (ratio > -1f)
                _cooldownGraphic.Visible = true;
            else
            {
                _cooldownGraphic.Visible = false;
                Action.Cooldown = null;
                DrawModeChanged();
            }
        }
    }
}
