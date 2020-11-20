#nullable enable
using System;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.Alert;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mobs
{
    public class AlertControl : BaseButton
    {
        public AlertPrototype Alert { get; }

        /// <summary>
        /// Total duration of the cooldown in seconds. Null if no duration / cooldown.
        /// </summary>
        public int? TotalDuration { get; set; }

        private short? _severity;
        private readonly TextureRect _icon;
        private CooldownGraphic _cooldownGraphic;

        private readonly IResourceCache _resourceCache;


        /// <summary>
        /// Creates an alert control reflecting the indicated alert + state
        /// </summary>
        /// <param name="alert">alert to display</param>
        /// <param name="severity">severity of alert, null if alert doesn't have severity levels</param>
        /// <param name="resourceCache">resourceCache to use to load alert icon textures</param>
        public AlertControl(AlertPrototype alert, short? severity, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;
            Alert = alert;
            _severity = severity;
            var texture = _resourceCache.GetTexture(alert.GetIconPath(_severity));
            _icon = new TextureRect
            {
                TextureScale = (2, 2),
                Texture = texture
            };

            Children.Add(_icon);
            _cooldownGraphic = new CooldownGraphic();
            Children.Add(_cooldownGraphic);

        }

        /// <summary>
        /// Change the alert severity, changing the displayed icon
        /// </summary>
        public void SetSeverity(short? severity)
        {
            if (_severity != severity)
            {
                _severity = severity;
                _icon.Texture = _resourceCache.GetTexture(Alert.GetIconPath(_severity));
            }
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
