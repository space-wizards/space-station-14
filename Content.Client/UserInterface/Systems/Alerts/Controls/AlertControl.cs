using System.Numerics;
using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Shared.Alert;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Alerts.Controls
{
    public sealed class AlertControl : BaseButton
    {
        public AlertPrototype Alert { get; }

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

        private short? _severity;
        private readonly IGameTiming _gameTiming;
        private readonly AnimatedTextureRect _icon;
        private readonly CooldownGraphic _cooldownGraphic;

        /// <summary>
        /// Creates an alert control reflecting the indicated alert + state
        /// </summary>
        /// <param name="alert">alert to display</param>
        /// <param name="severity">severity of alert, null if alert doesn't have severity levels</param>
        public AlertControl(AlertPrototype alert, short? severity)
        {
            _gameTiming = IoCManager.Resolve<IGameTiming>();
            TooltipSupplier = SupplyTooltip;
            Alert = alert;
            _severity = severity;
            var specifier = alert.GetIcon(_severity);
            _icon = new AnimatedTextureRect
            {
                DisplayRect = {TextureScale = new Vector2(2, 2)}
            };

            _icon.SetFromSpriteSpecifier(specifier);

            Children.Add(_icon);
            _cooldownGraphic = new CooldownGraphic();
            Children.Add(_cooldownGraphic);
        }

        private Control SupplyTooltip(Control? sender)
        {
            var msg = FormattedMessage.FromMarkup(Loc.GetString(Alert.Name));
            var desc = FormattedMessage.FromMarkup(Loc.GetString(Alert.Description));
            return new ActionAlertTooltip(msg, desc) {Cooldown = Cooldown};
        }

        /// <summary>
        /// Change the alert severity, changing the displayed icon
        /// </summary>
        public void SetSeverity(short? severity)
        {
            if (_severity != severity)
            {
                _severity = severity;
                _icon.SetFromSpriteSpecifier(Alert.GetIcon(_severity));
            }
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

            _cooldownGraphic.FromTime(Cooldown.Value.Start, Cooldown.Value.End);
        }
    }
}
