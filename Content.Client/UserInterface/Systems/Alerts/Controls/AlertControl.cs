using System.Numerics;
using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Shared.Alert;
using Robust.Client.GameObjects;
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
        private readonly IEntityManager _entityManager;
        private readonly SpriteView _icon;
        private readonly CooldownGraphic _cooldownGraphic;

        private EntityUid _spriteViewEntity;

        /// <summary>
        /// Creates an alert control reflecting the indicated alert + state
        /// </summary>
        /// <param name="alert">alert to display</param>
        /// <param name="severity">severity of alert, null if alert doesn't have severity levels</param>
        public AlertControl(AlertPrototype alert, short? severity)
        {
            _gameTiming = IoCManager.Resolve<IGameTiming>();
            _entityManager = IoCManager.Resolve<IEntityManager>();
            TooltipSupplier = SupplyTooltip;
            Alert = alert;
            _severity = severity;

            _spriteViewEntity = _entityManager.Spawn(Alert.AlertViewEntity);
            if (_entityManager.TryGetComponent<SpriteComponent>(_spriteViewEntity, out var sprite))
            {
                var icon = Alert.GetIcon(_severity);
                if (sprite.LayerMapTryGet(AlertVisualLayers.Base, out var layer))
                    sprite.LayerSetSprite(layer, icon);
            }

            _icon = new SpriteView
            {
                Scale = new Vector2(2, 2)
            };
            _icon.SetEntity(_spriteViewEntity);

            Children.Add(_icon);
            _cooldownGraphic = new CooldownGraphic
            {
                MaxSize = new Vector2(64, 64)
            };
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
            if (_severity == severity)
                return;
            _severity = severity;

            if (!_entityManager.TryGetComponent<SpriteComponent>(_spriteViewEntity, out var sprite))
                return;
            var icon = Alert.GetIcon(_severity);
            if (sprite.LayerMapTryGet(AlertVisualLayers.Base, out var layer))
                sprite.LayerSetSprite(layer, icon);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UserInterfaceManager.GetUIController<AlertsUIController>().UpdateAlertSpriteEntity(_spriteViewEntity, Alert);

            if (!Cooldown.HasValue)
            {
                _cooldownGraphic.Visible = false;
                _cooldownGraphic.Progress = 0;
                return;
            }

            _cooldownGraphic.FromTime(Cooldown.Value.Start, Cooldown.Value.End);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_entityManager.Deleted(_spriteViewEntity))
                _entityManager.QueueDeleteEntity(_spriteViewEntity);
        }
    }

    public enum AlertVisualLayers : byte
    {
        Base
    }
}
