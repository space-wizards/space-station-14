using System;
using System.Collections.Generic;
using Content.Shared.Alert;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Handles the icons on the right side of the screen.
    /// Should only be used for player-controlled entities
    /// </summary>
    public abstract class SharedAlertsComponent : Component
    {
        [Dependency]
        protected readonly AlertManager AlertManager = default!;

        public override string Name => "AlertsUI";
        public override uint? NetID => ContentNetIDs.ALERTS;

        [ViewVariables]
        private Dictionary<AlertSlot, AlertState> _alerts = new Dictionary<AlertSlot, AlertState>();

        public IReadOnlyDictionary<AlertSlot, AlertState> Alerts => _alerts;

        // TODO: Remove
        protected void SetAlerts(IReadOnlyDictionary<AlertSlot, AlertState> newAlerts)
        {
            _alerts = new Dictionary<AlertSlot, AlertState>(newAlerts);
        }

        /// <summary>
        /// Shows the alert. If the alert or another alert of the same category is already showing,
        /// it will be updated with the specified values.
        /// </summary>
        /// <param name="alertId">id of the alert to set</param>
        /// <param name="severity">severity, if supported by the alert</param>
        /// <param name="cooldown">cooldown start and end, if null there will be no cooldown (and it will
        /// be erased if there is currently a cooldown for the alert)</param>
        public void ShowAlert(string alertId, short? severity = null,
            ValueTuple<TimeSpan, TimeSpan>? cooldown = null)
        {
            if (AlertManager.TryGetWithEncoded(alertId, out var alert, out var encoded))
            {
                //TODO: All these duplicated modified checks should be refactored between this and ServerAlertsComponent
                if (_alerts.TryGetValue(alert.AlertSlot, out var value) &&
                    value.AlertEncoded == encoded &&
                    value.Severity == severity && value.Cooldown == cooldown)
                {
                    return;
                }
                _alerts[alert.AlertSlot] = new AlertState()
                    {Cooldown = cooldown, AlertEncoded = encoded, Severity = severity};
                Dirty();

            }
            else
            {
                Logger.ErrorS("alert", "Unable to show alert {0}, please ensure this is a valid alertId",
                    alertId);
            }
        }

        // TODO: by category or by id
        public void ClearAlert(AlertSlot effect)
        {
            if (!_alerts.Remove(effect))
            {
                return;
            }

            AfterClearAlert(effect);

            Dirty();
        }

        /// <summary>
        /// Invoked after clearing an alert prior to dirtying the control
        /// </summary>
        protected virtual void AfterClearAlert(AlertSlot effect) { }
    }

    [Serializable, NetSerializable]
    public class AlertsComponentState : ComponentState
    {
        // TODO: not sure a dict is really the best way to transmit this
        public IReadOnlyDictionary<AlertSlot, AlertState> Alerts;

        public AlertsComponentState(IReadOnlyDictionary<AlertSlot, AlertState> alerts) : base(ContentNetIDs.ALERTS)
        {
            Alerts = alerts;
        }
    }

    /// <summary>
    /// A message that calls the click interaction on a alert
    /// </summary>
    [Serializable, NetSerializable]
    public class ClickAlertMessage : ComponentMessage
    {
        public readonly AlertSlot Effect;

        public ClickAlertMessage(AlertSlot effect)
        {
            Directed = true;
            Effect = effect;
        }
    }

    [Serializable, NetSerializable]
    public struct AlertState
    {
        public int AlertEncoded;
        public short? Severity;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
    }

    public enum AlertSlot
    {
        Health,
        Hunger,
        Thirst,
        Pressure,
        Fire,
        Temperature,
        Stun,
        Cuffed,
        Buckled,
        Piloting,
        Pulling,
        Pulled,
        Weightless,
        Error
    }
}
