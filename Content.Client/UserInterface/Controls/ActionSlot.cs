#nullable enable
using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Actions;
using Content.Shared.Alert;
using OpenToolkit.Mathematics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    /// A slot in the action hotbar
    /// </summary>
    public class ActionSlot : BaseButton
    {
        /// <summary>
        /// Current action in this slot.
        /// </summary>
        public ActionPrototype? Action { get; }

        /// <summary>
        /// Whether the action in this slot is currently shown as granted (enabled).
        /// </summary>
        public bool Granted { get; }

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

        private readonly IResourceCache _resourceCache;


        /// <summary>
        /// Creates an action slot for the specified number
        /// </summary>
        /// <param name="slotNumber">slot this corresponds to, 1-10 (10 is labeled as 0)</param>
        /// <param name="resourceCache">resource cache to use to load textures</param>
        public ActionSlot(byte slotNumber, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            SlotNumber = slotNumber;

            // create the background and number in the corner.
            var panel = new PanelContainer
            {
                StyleClasses = {StyleNano.StyleClassLightBorderedPanel},
                CustomMinimumSize = (64, 64)
            };


            _number = new RichTextLabel();
            // TODO: Position in corner (use a sub-parent layoutcontainer??)
            _number.SetMessage(slotNumber == 10 ? "0" : slotNumber.ToString());
            _icon = new TextureRect {TextureScale = (2, 2), Visible = false};
            Children.Add(_icon);
            _cooldownGraphic = new CooldownGraphic();
            Children.Add(_cooldownGraphic);

        }

        /// <summary>
        /// Updates the displayed cooldown amount, doing nothing if alertCooldown is null
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
    }
}
