#nullable enable
using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using OpenToolkit.Mathematics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    /// A slot in the action hotbar
    /// </summary>
    public class ActionSlot : ContainerButton
    {
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

        /// <summary>
        /// 1-10 corresponding to the number label on the slot (10 is labeled as 0)
        /// </summary>
        public byte SlotNumber { get; private set; }

        /// <summary>
        /// Total duration of the cooldown in seconds. Null if no duration / cooldown.
        /// </summary>
        public int? TotalDuration { get; set; }

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

            // initially has no action, should not be interactable
            Disabled = true;
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
                TotalDuration = null;
            }
            else
            {

                var start = alertCooldown.Value.Start;
                var end = alertCooldown.Value.End;

                var length = (end - start).TotalSeconds;
                var progress = (curTime - start).TotalSeconds / length;
                var ratio = (progress <= 1 ? (1 - progress) : (curTime - end).TotalSeconds * -5);

                TotalDuration = (int?) Math.Round(length);
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
            Action = action;
            _icon.Texture = Action.Icon.Frame0();
            _icon.Visible = true;
            Grant();
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as granted
        /// </summary>
        public void Grant()
        {
            if (Action == null || Granted) return;

            Disabled = false;
            Granted = true;
            _number.SetMessage(SlotNumberLabel());
        }

        /// <summary>
        /// Display the action in this slot (if there is one) as revoked
        /// </summary>
        public void Revoke()
        {
            if (Action == null || !Granted) return;

            Granted = false;
            Disabled = true;
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
            // when there's no action, it should not be interactable but it should show
            // the normal style, not the "disabled" style
            if (DrawMode == DrawModeEnum.Disabled && Action == null)
            {
                SetOnlyStylePseudoClass(StylePseudoClassNormal);
            }
        }
    }
}
