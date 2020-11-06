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
        protected Dictionary<AlertKey, AlertState> Alerts = new Dictionary<AlertKey, AlertState>();

        /// <returns>true iff an alert of the indicated alert category is currently showing</returns>
        public bool IsShowingAlertCategory(string alertCategory)
        {
            return Alerts.ContainsKey(AlertKey.ForCategory(alertCategory));
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
                if (Alerts.TryGetValue(alert.AlertKey, out var value) &&
                    value.AlertEncoded == encoded &&
                    value.Severity == severity && value.Cooldown == cooldown)
                {
                    return;
                }
                Alerts[alert.AlertKey] = new AlertState()
                    {Cooldown = cooldown, AlertEncoded = encoded, Severity = severity};
                Dirty();

            }
            else
            {
                Logger.ErrorS("alert", "Unable to show alert {0}, please ensure this is a valid alertId",
                    alertId);
            }
        }

        /// <summary>
        /// Clear the alert with the given category, if one is currently showing.
        /// </summary>
        public void ClearAlertCategory(string category)
        {
            if (!Alerts.Remove(AlertKey.ForCategory(category)))
            {
                return;
            }

            AfterClearAlert();

            Dirty();
        }

        /// <summary>
        /// Clear the alert with the given id.
        /// </summary>
        /// <param name="alertId"></param>
        public void ClearAlert(string alertId)
        {
            if (AlertManager.TryGet(alertId, out var alert))
            {
                if (!Alerts.Remove(alert.AlertKey))
                {
                    return;
                }

                AfterClearAlert();

                Dirty();
            }
            else
            {
                Logger.ErrorS("alert", "unable to clear alert, unknown alert id {0}", alertId);
            }

        }

        /// <summary>
        /// Invoked after clearing an alert prior to dirtying the control
        /// </summary>
        protected virtual void AfterClearAlert() { }
    }

    [Serializable, NetSerializable]
    public class AlertsComponentState : ComponentState
    {
        public AlertState[] Alerts;

        public AlertsComponentState(AlertState[] alerts) : base(ContentNetIDs.ALERTS)
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
        public readonly int EncodedAlert;

        public ClickAlertMessage(int encodedAlert)
        {
            Directed = true;
            EncodedAlert = encodedAlert;
        }
    }

    [Serializable, NetSerializable]
    public struct AlertState
    {
        public int AlertEncoded;
        public short? Severity;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
    }
}
