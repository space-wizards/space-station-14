using System;
using Content.Client.Cooldown;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Prototypes;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Actions.UI
{
    /// <summary>
    /// A slot in the action hotbar. Not extending BaseButton because
    /// its needs diverged too much.
    /// </summary>
    public class ActionSlot : PanelContainer
    {
        // shorter than default tooltip delay so user can more easily
        // see what actions they've been given
        private const float CustomTooltipDelay = 0.5f;

        private static readonly string EnabledColor = "#7b7e9e";
        private static readonly string DisabledColor = "#950000";

        /// <summary>
        /// Current action in this slot.
        /// </summary>
        public BaseActionPrototype? Action { get; private set; }

        /// <summary>
        /// true if there is an action assigned to the slot
        /// </summary>
        public bool HasAssignment => Action != null;

        private bool HasToggleSprite => Action != null && Action.IconOn != SpriteSpecifier.Invalid;

        /// <summary>
        /// Only applicable when an action is in this slot.
        /// True if the action is currently shown as enabled, false if action disabled.
        /// </summary>
        public bool ActionEnabled { get; private set; }

        /// <summary>
        /// Is there an action in the slot that can currently be used?
        /// Target-basedActions on cooldown can still be selected / deselected if they've been configured as such
        /// </summary>
        public bool CanUseAction => Action != null && ActionEnabled &&
                                    (!IsOnCooldown || (Action.IsTargetAction && !Action.DeselectOnCooldown));

        /// <summary>
        /// Item the action is provided by, only valid if Action is an ItemActionPrototype. May be null
        /// if the item action is not yet tied to an item.
        /// </summary>
        public EntityUid? Item { get; private set; }

        /// <summary>
        /// Whether the action in this slot should be shown as toggled on. Separate from Depressed.
        /// </summary>
        public bool ToggledOn
        {
            get => _toggledOn;
            set
            {
                if (_toggledOn == value) return;
                _toggledOn = value;
                UpdateIcons();
                DrawModeChanged();
            }
        }

        /// <summary>
        /// 1-10 corresponding to the number label on the slot (10 is labeled as 0)
        /// </summary>
        private byte SlotNumber => (byte) (SlotIndex + 1);
        public byte SlotIndex { get; }

        /// <summary>
        /// Current cooldown displayed in this slot. Set to null to show no cooldown.
        /// </summary>
        public (TimeSpan Start, TimeSpan End)? Cooldown
        {
            get => _cooldown;
            set
            {
                _cooldown = value;
                if (SuppliedTooltip is ActionAlertTooltip actionAlertTooltip)
                {
                    actionAlertTooltip.Cooldown = value;
                }
            }
        }
        private (TimeSpan Start, TimeSpan End)? _cooldown;

        public bool IsOnCooldown => Cooldown.HasValue && _gameTiming.CurTime < Cooldown.Value.End;

        private readonly IGameTiming _gameTiming;
        private readonly RichTextLabel _number;
        private readonly TextureRect _bigActionIcon;
        private readonly TextureRect _smallActionIcon;
        private readonly SpriteView _smallItemSpriteView;
        private readonly SpriteView _bigItemSpriteView;
        private readonly CooldownGraphic _cooldownGraphic;
        private readonly ActionsUI _actionsUI;
        private readonly ActionMenu _actionMenu;
        private readonly ClientActionsComponent _actionsComponent;
        private bool _toggledOn;
        // whether button is currently pressed down by mouse or keybind down.
        private bool _depressed;
        private bool _beingHovered;

        /// <summary>
        /// Creates an action slot for the specified number
        /// </summary>
        /// <param name="slotIndex">slot index this corresponds to, 0-9 (0 labeled as 1, 8, labeled "9", 9 labeled as "0".</param>
        public ActionSlot(ActionsUI actionsUI, ActionMenu actionMenu, ClientActionsComponent actionsComponent, byte slotIndex)
        {
            _actionsComponent = actionsComponent;
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
                Visible = false
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
            return Action == null ? null :
                new ActionAlertTooltip(Action.Name, Action.Description, Action.Requires) {Cooldown = Cooldown};
        }

        /// <summary>
        /// Action attempt for performing the action in the slot
        /// </summary>
        public IActionAttempt? ActionAttempt()
        {
            IActionAttempt? attempt = Action switch
            {
                ActionPrototype actionPrototype => new ActionAttempt(actionPrototype),
                ItemActionPrototype itemActionPrototype =>
                    Item.HasValue && IoCManager.Resolve<IEntityManager>().TryGetComponent<ItemActionsComponent?>(Item, out var itemActions) ?
                        new ItemActionAttempt(itemActionPrototype, Item.Value, itemActions) : null,
                _ => null
            };
            return attempt;
        }

        protected override void MouseEntered()
        {
            base.MouseEntered();

            _beingHovered = true;
            DrawModeChanged();
            if (Action is not ItemActionPrototype) return;
            if (Item == null) return;
            _actionsComponent.HighlightItemSlot(Item.Value);
        }

        protected override void MouseExited()
        {
            base.MouseExited();
            _beingHovered = false;
            CancelPress();
            DrawModeChanged();
            _actionsComponent.StopHighlightingItemSlots();
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function == EngineKeyFunctions.UIRightClick)
            {
                if (!_actionsUI.Locked && !_actionsUI.DragDropHelper.IsDragging && !_actionMenu.IsDragging)
                {
                    _actionsComponent.Assignments.ClearSlot(_actionsUI.SelectedHotbar, SlotIndex, true);
                    _actionsUI.StopTargeting();
                    _actionsUI.UpdateUI();
                }
                return;
            }

            // only handle clicks, and can't do anything to this if no assignment
            if (args.Function != EngineKeyFunctions.UIClick || !HasAssignment)
                return;

            // might turn into a drag or a full press if released
            Depress(true);
            _actionsUI.DragDropHelper.MouseDown(this);
            DrawModeChanged();
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
                var fromAssignment = _actionsComponent.Assignments[_actionsUI.SelectedHotbar, fromIdx];
                var toIdx = targetSlot.SlotIndex;
                var toAssignment = _actionsComponent.Assignments[_actionsUI.SelectedHotbar, toIdx];

                if (fromIdx == toIdx) return;
                if (!fromAssignment.HasValue) return;

                _actionsComponent.Assignments.AssignSlot(_actionsUI.SelectedHotbar, toIdx, fromAssignment.Value);
                if (toAssignment.HasValue)
                {
                    _actionsComponent.Assignments.AssignSlot(_actionsUI.SelectedHotbar, fromIdx, toAssignment.Value);
                }
                else
                {
                    _actionsComponent.Assignments.ClearSlot(_actionsUI.SelectedHotbar, fromIdx, false);
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
        /// trigger the action. Only has an effect if CanUseAction.
        /// </summary>
        public void Depress(bool depress)
        {
            // action can still be toggled if it's allowed to stay selected
            if (!CanUseAction) return;


            if (_depressed && !depress)
            {
                // fire the action
                // no left-click interaction with it on cooldown or revoked
                _actionsComponent.AttemptAction(this);
            }
            _depressed = depress;
           DrawModeChanged();
        }

        /// <summary>
        /// Updates the action assigned to this slot.
        /// </summary>
        /// <param name="action">action to assign</param>
        /// <param name="actionEnabled">whether action should initially appear enable or disabled</param>
        public void Assign(ActionPrototype action, bool actionEnabled)
        {
            // already assigned
            if (Action != null && Action == action) return;

            Action = action;
            Item = default;
            _depressed = false;
            ToggledOn = false;
            ActionEnabled = actionEnabled;
            Cooldown = null;
            HideTooltip();
            UpdateIcons();
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Updates the item action assigned to this slot. The action will always be shown as disabled
        /// until it is tied to a specific item.
        /// </summary>
        /// <param name="action">action to assign</param>
        public void Assign(ItemActionPrototype action)
        {
            // already assigned
            if (Action != null && Action == action && Item == default) return;

            Action = action;
            Item = default;
            _depressed = false;
            ToggledOn = false;
            ActionEnabled = false;
            Cooldown = null;
            HideTooltip();
            UpdateIcons();
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Updates the item action assigned to this slot, tied to a specific item.
        /// </summary>
        /// <param name="action">action to assign</param>
        /// <param name="item">item the action is provided by</param>
        /// <param name="actionEnabled">whether action should initially appear enable or disabled</param>
        public void Assign(ItemActionPrototype action, EntityUid item, bool actionEnabled)
        {
            // already assigned
            if (Action != null && Action == action && Item == item) return;

            Action = action;
            Item = item;
            _depressed = false;
            ToggledOn = false;
            ActionEnabled = false;
            Cooldown = null;
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
            if (!HasAssignment) return;
            Action = null;
            Item = default;
            ToggledOn = false;
            _depressed = false;
            Cooldown = null;
            HideTooltip();
            UpdateIcons();
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as enabled
        /// </summary>
        public void EnableAction()
        {
            if (ActionEnabled || !HasAssignment) return;

            ActionEnabled = true;
            _depressed = false;
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as disabled.
        /// The slot is still clickable.
        /// </summary>
        public void DisableAction()
        {
            if (!ActionEnabled || !HasAssignment) return;

            ActionEnabled = false;
            _depressed = false;
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        private FormattedMessage SlotNumberLabel()
        {
            if (SlotNumber > 10) return FormattedMessage.FromMarkup("");
            var number = Loc.GetString(SlotNumber == 10 ? "0" : SlotNumber.ToString());
            var color = (ActionEnabled || !HasAssignment) ? EnabledColor : DisabledColor;
            return FormattedMessage.FromMarkup("[color=" + color + "]" + number + "[/color]");
        }

        private void UpdateIcons()
        {
            if (!HasAssignment)
            {
                SetActionIcon(null);
                SetItemIcon(null);
                return;
            }

            if (HasToggleSprite && ToggledOn && Action != null)
            {
                SetActionIcon(Action.IconOn.Frame0());
            }
            else if (Action != null)
            {
                SetActionIcon(Action.Icon.Frame0());
            }

            if (Item != default)
            {
                SetItemIcon(IoCManager.Resolve<IEntityManager>().TryGetComponent<ISpriteComponent?>(Item, out var spriteComponent) ? spriteComponent : null);
            }
            else
            {
                SetItemIcon(null);
            }
        }

        private void SetActionIcon(Texture? texture)
        {
            if (texture == null || !HasAssignment)
            {
                _bigActionIcon.Texture = null;
                _bigActionIcon.Visible = false;
                _smallActionIcon.Texture = null;
                _smallActionIcon.Visible = false;
            }
            else
            {
                if (Action is ItemActionPrototype {IconStyle: ItemActionIconStyle.BigItem})
                {
                    _bigActionIcon.Texture = null;
                    _bigActionIcon.Visible = false;
                    _smallActionIcon.Texture = texture;
                    _smallActionIcon.Visible = true;
                }
                else
                {
                    _bigActionIcon.Texture = texture;
                    _bigActionIcon.Visible = true;
                    _smallActionIcon.Texture = null;
                    _smallActionIcon.Visible = false;
                }

            }
        }

        private void SetItemIcon(ISpriteComponent? sprite)
        {
            if (sprite == null || !HasAssignment)
            {
                _bigItemSpriteView.Visible = false;
                _bigItemSpriteView.Sprite = null;
                _smallItemSpriteView.Visible = false;
                _smallItemSpriteView.Sprite = null;
            }
            else
            {
                if (Action is ItemActionPrototype actionPrototype)
                {
                    switch (actionPrototype.IconStyle)
                    {
                        case ItemActionIconStyle.BigItem:
                        {
                            _bigItemSpriteView.Visible = true;
                            _bigItemSpriteView.Sprite = sprite;
                            _smallItemSpriteView.Visible = false;
                            _smallItemSpriteView.Sprite = null;
                            break;
                        }
                        case ItemActionIconStyle.BigAction:
                        {
                            _bigItemSpriteView.Visible = false;
                            _bigItemSpriteView.Sprite = null;
                            _smallItemSpriteView.Visible = true;
                            _smallItemSpriteView.Sprite = sprite;
                            break;
                        }
                        case ItemActionIconStyle.NoItem:
                        {
                            _bigItemSpriteView.Visible = false;
                            _bigItemSpriteView.Sprite = null;
                            _smallItemSpriteView.Visible = false;
                            _smallItemSpriteView.Sprite = null;
                            break;
                        }
                    }

                }
                else
                {
                    _bigItemSpriteView.Visible = false;
                    _bigItemSpriteView.Sprite = null;
                    _smallItemSpriteView.Visible = false;
                    _smallItemSpriteView.Sprite = null;
                }

            }
        }


        private void DrawModeChanged()
        {

            // show a hover only if the action is usable or another action is being dragged on top of this
            if (_beingHovered)
            {
                if (_actionsUI.DragDropHelper.IsDragging || _actionMenu.IsDragging ||
                    (HasAssignment && ActionEnabled && !IsOnCooldown))
                {
                    SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassHover);
                    return;
                }
            }

            // always show the normal empty button style if no action in this slot
            if (!HasAssignment)
            {
                SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
                return;
            }

            // it's only depress-able if it's usable, so if we're depressed
            // show the depressed style
            if (_depressed)
            {
                SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassPressed);
                return;
            }


            // if it's toggled on, always show the toggled on style (currently same as depressed style)
            if (ToggledOn)
            {
                // when there's a toggle sprite, we're showing that sprite instead of highlighting this slot
                SetOnlyStylePseudoClass(HasToggleSprite ? ContainerButton.StylePseudoClassNormal :
                    ContainerButton.StylePseudoClassPressed);
                return;
            }


            if (!ActionEnabled)
            {
                SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassDisabled);
                return;
            }


            SetOnlyStylePseudoClass(ContainerButton.StylePseudoClassNormal);
        }



        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (!Cooldown.HasValue)
            {
                _cooldownGraphic.Visible = false;
                _cooldownGraphic.Progress = 0;
                return;
            }

            var duration = Cooldown.Value.End - Cooldown.Value.Start;
            var curTime = _gameTiming.CurTime;
            var length = duration.TotalSeconds;
            var progress = (curTime - Cooldown.Value.Start).TotalSeconds / length;
            var ratio = (progress <= 1 ? (1 - progress) : (curTime - Cooldown.Value.End).TotalSeconds * -5);

            _cooldownGraphic.Progress = MathHelper.Clamp((float)ratio, -1, 1);
            _cooldownGraphic.Visible = ratio > -1f;
        }
    }
}
