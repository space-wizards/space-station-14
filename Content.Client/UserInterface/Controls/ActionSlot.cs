#nullable enable
using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using OpenToolkit.Mathematics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    /// A slot in the action hotbar.
    /// Note that this should never be Disabled internally, it always needs to be clickable regardless
    /// of whether the action is revoked (so actions can still be dragged / unassigned).
    /// Thus any event handlers should check if the action is granted.
    /// </summary>
    public class ActionSlot : ContainerButton
    {
        // shorter than default tooltip delay so user can more easily
        // see what actions they've been given
        private const float CustomTooltipDelay = 0.5f;

        private static readonly string GrantedColor = "#7b7e9e";
        private static readonly string RevokedColor = "#950000";

        /// <summary>
        /// Current action in this slot.
        /// </summary>
        public ActionPrototype? Action { get; private set; }

        /// <summary>
        /// Whether the action in this slot is currently shown as granted (enabled).
        /// </summary>
        public bool Granted { get; private set; }

        private bool _toggledOn;

        /// <summary>
        /// Separate from Pressed, if true, this button will be displayed as pressed
        /// regardless of the Pressed setting.
        /// </summary>
        public bool ToggledOn
        {
            get => _toggledOn;
            set
            {
                if (_toggledOn == value) return;
                _toggledOn = value;
                DrawModeChanged();
            }

        }

        /// <summary>
        /// 1-10 corresponding to the number label on the slot (10 is labeled as 0)
        /// </summary>
        public byte SlotNumber { get; private set; }
        public byte SlotIndex => (byte) (SlotNumber - 1);

        /// <summary>
        /// Total duration of the current cooldown in seconds. TimeSpan.Zero if no duration / cooldown.
        /// </summary>
        public TimeSpan TotalDuration { get; private set; }
        /// <summary>
        /// Remaining cooldown in seconds. TimeSpan.Zero if no cooldown or cooldown
        /// is over.
        /// </summary>
        public TimeSpan CooldownRemaining { get; private set; }

        public bool IsOnCooldown => CooldownRemaining != TimeSpan.Zero;

        private readonly RichTextLabel _number;
        private readonly TextureRect _icon;
        private readonly CooldownGraphic _cooldownGraphic;

        /// <summary>
        /// Creates an action slot for the specified number
        /// </summary>
        /// <param name="slotNumber">slot this corresponds to, 1-10 (10 is labeled as 0). Any value
        /// greater than 10 will have a blank number</param>
        public ActionSlot(byte slotNumber)
        {
            SlotNumber = slotNumber;

            CustomMinimumSize = (64, 64);

            SizeFlagsVertical = SizeFlags.None;
            TooltipDelay = CustomTooltipDelay;

            _number = new RichTextLabel
            {
                StyleClasses = {StyleNano.StyleClassHotbarSlotNumber}
            };
            _number.SetMessage(SlotNumberLabel());

            _icon = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false
            };
            _cooldownGraphic = new CooldownGraphic();

            // padding to the left of the number to shift it right
            var paddingBox = new HBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                CustomMinimumSize = (64, 64)
            };
            paddingBox.AddChild(new Control()
            {
                CustomMinimumSize = (4, 4),
                SizeFlagsVertical = SizeFlags.Fill
            });
            paddingBox.AddChild(_number);
            AddChild(paddingBox);
            AddChild(_icon);
            AddChild(_cooldownGraphic);

            UpdateCooldown(null, TimeSpan.Zero);

        }

        /// <summary>
        /// Updates the displayed cooldown amount, clearing cooldown if alertCooldown is null
        /// </summary>
        /// <param name="alertCooldown">cooldown start and end</param>
        /// <param name="curTime">current game time</param>
        public void UpdateCooldown((TimeSpan Start, TimeSpan End)? alertCooldown, in TimeSpan curTime)
        {
            if (!alertCooldown.HasValue)
            {
                _cooldownGraphic.Progress = 0;
                _cooldownGraphic.Visible = false;
                TotalDuration = TimeSpan.Zero;
                CooldownRemaining = TimeSpan.Zero;
            }
            else
            {

                var start = alertCooldown.Value.Start;
                var end = alertCooldown.Value.End;

                TotalDuration = end - start;
                var length = TotalDuration.TotalSeconds;
                var progress = (curTime - start).TotalSeconds / length;
                var ratio = (progress <= 1 ? (1 - progress) : (curTime - end).TotalSeconds * -5);

                CooldownRemaining = end > curTime ? (end - curTime) : TimeSpan.Zero;

                _cooldownGraphic.Progress = MathHelper.Clamp((float)ratio, -1, 1);
                _cooldownGraphic.Visible = ratio > -1f;
            }
        }

        /// <summary>
        /// Updates the action assigned to this slot.
        /// </summary>
        /// <param name="action">action to assign</param>
        public void Assign(ActionPrototype action)
        {
            // already assigned
            if (Action != null && Action.ActionType == action.ActionType) return;

            Action = action;
            _icon.Texture = Action.Icon.Frame0();
            _icon.Visible = true;
            Pressed = false;
            ToggledOn = false;
            Granted = true;
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
            _icon.Texture = null;
            _icon.Visible = false;
            ToggledOn = false;
            Pressed = false;
            DrawModeChanged();
            UpdateCooldown(null, TimeSpan.Zero);
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as granted
        /// </summary>
        public void Grant()
        {
            if (Action == null || Granted) return;

            Granted = true;
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as revoked
        /// </summary>
        public void Revoke()
        {
            if (Action == null || !Granted) return;

            Granted = false;
            DrawModeChanged();
            _number.SetMessage(SlotNumberLabel());
            UpdateCooldown(null, TimeSpan.Zero);
        }

        private FormattedMessage SlotNumberLabel()
        {
            if (SlotNumber > 10) return FormattedMessage.FromMarkup("");
            var number = SlotNumber == 10 ? "0" : SlotNumber.ToString();
            var color = (Granted || Action == null) ? GrantedColor : RevokedColor;
            return FormattedMessage.FromMarkup("[color=" + color + "]" + number + "[/color]");
        }

        protected override void DrawModeChanged()
        {
            base.DrawModeChanged();
            // when there's no action or its on cooldown or revoked, it should
            // not appear as if it's interactable (no mouseover or press style)
            if (Action == null)
            {
                SetOnlyStylePseudoClass(StylePseudoClassNormal);
            }
            else if (_cooldownGraphic.Visible && Granted)
            {
                SetOnlyStylePseudoClass(ToggledOn ? StylePseudoClassPressed : StylePseudoClassNormal);
            }
            else if (!Granted)
            {
                SetOnlyStylePseudoClass(ToggledOn ? StylePseudoClassPressed : StylePseudoClassDisabled);
            }
            else if (DrawMode != DrawModeEnum.Hover && ToggledOn)
            {
                SetOnlyStylePseudoClass(StylePseudoClassPressed);
            }
        }

        /// <summary>
        /// Simulates clicking on this, but being done via a keybind
        /// </summary>
        public void HandleKeybind(BoundKeyState keyState)
        {
            // simulate a click, using UIClick so it won't be treated as a possible drag / drop attempt
            var guiArgs = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick,
                keyState, new ScreenCoordinates(GlobalPixelPosition), true,
                default,
                default);
            if (keyState == BoundKeyState.Down)
            {
                KeyBindDown(guiArgs);
            }
            else
            {
                KeyBindUp(guiArgs);
            }

        }
    }
}
