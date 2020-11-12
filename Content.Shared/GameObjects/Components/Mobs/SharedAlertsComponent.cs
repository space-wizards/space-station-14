using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Alert;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Handles the icons on the right side of the screen.
    /// Should only be used for player-controlled entities.
    /// </summary>
    public abstract class SharedAlertsComponent : Component
    {
        private static readonly AlertState[] NO_ALERTS = new AlertState[0];

        [Dependency]
        protected readonly AlertManager AlertManager = default!;

        public override string Name => "AlertsUI";
        public override uint? NetID => ContentNetIDs.ALERTS;

        [ViewVariables]
        private Dictionary<AlertKey, ClickableAlertState> _alerts = new Dictionary<AlertKey, ClickableAlertState>();

        /// <returns>true iff an alert of the indicated alert category is currently showing</returns>
        public bool IsShowingAlertCategory(AlertCategory alertCategory)
        {
            return IsShowingAlert(AlertKey.ForCategory(alertCategory));
        }

        /// <returns>true iff an alert of the indicated id is currently showing</returns>
        public bool IsShowingAlert(AlertType alertType)
        {
            if (AlertManager.TryGet(alertType, out var alert))
            {
                return IsShowingAlert(alert.AlertKey);
            }
            Logger.DebugS("alert", "unknown alert type {0}", alertType);
            return false;

        }

        /// <returns>true iff an alert of the indicated key is currently showing</returns>
        protected bool IsShowingAlert(AlertKey alertKey)
        {
            return _alerts.ContainsKey(alertKey);
        }

        protected IEnumerable<AlertState> EnumerateAlertStates()
        {
            return _alerts.Values.Select(alertData => alertData.AlertState);
        }

        /// <summary>
        /// Invokes the alert's specified callback if there is one.
        /// Not intended to be used on clientside.
        /// </summary>
        protected void PerformAlertClickCallback(AlertPrototype alert, IEntity owner)
        {
            if (_alerts.TryGetValue(alert.AlertKey, out var alertStateCallback))
            {
                alertStateCallback.OnClickAlert?.Invoke(new ClickAlertEventArgs(owner, alert));
            }
            else
            {
                Logger.DebugS("alert", "player {0} attempted to invoke" +
                                       " alert click for {1} but that alert is not currently" +
                                       " showing", owner.Name, alert.AlertType);
            }
        }

        /// <summary>
        /// Creates a new array containing all of the current alert states.
        /// </summary>
        /// <returns></returns>
        protected AlertState[] CreateAlertStatesArray()
        {
            if (_alerts.Count == 0) return NO_ALERTS;
            var states = new AlertState[_alerts.Count];
            // because I don't trust LINQ
            var idx = 0;
            foreach (var alertData in _alerts.Values)
            {
                states[idx++] = alertData.AlertState;
            }

            return states;
        }

        protected bool TryGetAlertState(AlertKey key, out AlertState alertState)
        {
            if (_alerts.TryGetValue(key, out var alertData))
            {
                alertState = alertData.AlertState;
                return true;
            }

            alertState = default;
            return false;
        }

        /// <summary>
        /// Replace the current active alerts with the specified alerts. Any
        /// OnClickAlert callbacks on the active alerts will be erased.
        /// </summary>
        protected void SetAlerts(AlertState[] alerts)
        {
            var newAlerts = new Dictionary<AlertKey, ClickableAlertState>();
            foreach (var alertState in alerts)
            {
                if (AlertManager.TryDecode(alertState.AlertEncoded, out var alert))
                {
                    newAlerts[alert.AlertKey] = new ClickableAlertState
                    {
                        AlertState = alertState
                    };
                }
                else
                {
                    Logger.ErrorS("alert", "unrecognized encoded alert {0}", alertState.AlertEncoded);
                }
            }

            _alerts = newAlerts;
        }

        /// <summary>
        /// Shows the alert. If the alert or another alert of the same category is already showing,
        /// it will be updated / replaced with the specified values.
        /// </summary>
        /// <param name="alertType">type of the alert to set</param>
        /// <param name="onClickAlert">callback to invoke when ClickAlertMessage is received by the server
        /// after being clicked by client. Has no effect when specified on the clientside.</param>
        /// <param name="severity">severity, if supported by the alert</param>
        /// <param name="cooldown">cooldown start and end, if null there will be no cooldown (and it will
        /// be erased if there is currently a cooldown for the alert)</param>
        public void ShowAlert(AlertType alertType, short? severity = null, OnClickAlert onClickAlert = null,
            ValueTuple<TimeSpan, TimeSpan>? cooldown = null)
        {
            if (AlertManager.TryGetWithEncoded(alertType, out var alert, out var encoded))
            {
                if (_alerts.TryGetValue(alert.AlertKey, out var alertStateCallback) &&
                    alertStateCallback.AlertState.AlertEncoded == encoded &&
                    alertStateCallback.AlertState.Severity == severity && alertStateCallback.AlertState.Cooldown == cooldown)
                {
                    alertStateCallback.OnClickAlert = onClickAlert;
                    return;
                }

                _alerts[alert.AlertKey] = new ClickableAlertState
                {
                    AlertState = new AlertState
                        {Cooldown = cooldown, AlertEncoded = encoded, Severity = severity},
                    OnClickAlert = onClickAlert
                };

                Dirty();

            }
            else
            {
                Logger.ErrorS("alert", "Unable to show alert {0}, please ensure this alertType has" +
                                       " a corresponding YML alert prototype",
                    alertType);
            }
        }

        /// <summary>
        /// Clear the alert with the given category, if one is currently showing.
        /// </summary>
        public void ClearAlertCategory(AlertCategory category)
        {
            var key = AlertKey.ForCategory(category);
            if (!_alerts.Remove(key))
            {
                return;
            }

            AfterClearAlert();

            Dirty();
        }

        /// <summary>
        /// Clear the alert of the given type if it is currently showing.
        /// </summary>
        public void ClearAlert(AlertType alertType)
        {
            if (AlertManager.TryGet(alertType, out var alert))
            {
                if (!_alerts.Remove(alert.AlertKey))
                {
                    return;
                }

                AfterClearAlert();

                Dirty();
            }
            else
            {
                Logger.ErrorS("alert", "unable to clear alert, unknown alertType {0}", alertType);
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
        public readonly byte EncodedAlert;

        public ClickAlertMessage(byte encodedAlert)
        {
            Directed = true;
            EncodedAlert = encodedAlert;
        }
    }

    [Serializable, NetSerializable]
    public struct AlertState
    {
        public byte AlertEncoded;
        public short? Severity;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
    }

    public struct ClickableAlertState
    {
        public AlertState AlertState;
        public OnClickAlert OnClickAlert;
    }

    public delegate void OnClickAlert(ClickAlertEventArgs args);

    public class ClickAlertEventArgs : EventArgs
    {
        /// <summary>
        /// Player clicking the alert
        /// </summary>
        public readonly IEntity Player;
        /// <summary>
        /// Alert that was clicked
        /// </summary>
        public readonly AlertPrototype Alert;

        public ClickAlertEventArgs(IEntity player, AlertPrototype alert)
        {
            Player = player;
            Alert = alert;
        }
    }
}
