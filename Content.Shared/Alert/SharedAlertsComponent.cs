using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Handles the icons on the right side of the screen.
    /// Should only be used for player-controlled entities.
    /// </summary>
    [NetworkedComponent()]
    public abstract class SharedAlertsComponent : Component
    {
        [Dependency]
        protected readonly AlertManager AlertManager = default!;

        public override string Name => "Alerts";

        [ViewVariables] private Dictionary<AlertKey, AlertState> _alerts = new();

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not AlertsComponentState state)
            {
                return;
            }

            _alerts = state.Alerts;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new AlertsComponentState(_alerts);
        }

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

        protected IEnumerable<KeyValuePair<AlertKey, AlertState>> EnumerateAlertStates()
        {
            return _alerts;
        }

        protected bool TryGetAlertState(AlertKey key, out AlertState alertState)
        {
            return _alerts.TryGetValue(key, out alertState);
        }

        /// <summary>
        /// Shows the alert. If the alert or another alert of the same category is already showing,
        /// it will be updated / replaced with the specified values.
        /// </summary>
        /// <param name="alertType">type of the alert to set</param>
        /// <param name="severity">severity, if supported by the alert</param>
        /// <param name="cooldown">cooldown start and end, if null there will be no cooldown (and it will
        /// be erased if there is currently a cooldown for the alert)</param>
        public void ShowAlert(AlertType alertType, short? severity = null, (TimeSpan, TimeSpan)? cooldown = null)
        {
            if (AlertManager.TryGet(alertType, out var alert))
            {
                // Check whether the alert category we want to show is already being displayed, with the same type,
                // severity, and cooldown.
                if (_alerts.TryGetValue(alert.AlertKey, out var alertStateCallback) &&
                    alertStateCallback.Type == alertType &&
                    alertStateCallback.Severity == severity &&
                    alertStateCallback.Cooldown == cooldown)
                {
                    return;
                }

                // In the case we're changing the alert type but not the category, we need to remove it first.
                _alerts.Remove(alert.AlertKey);

                _alerts[alert.AlertKey] = new AlertState
                    {Cooldown = cooldown, Severity = severity, Type=alertType};

                AfterShowAlert();

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
        /// Invoked after showing an alert prior to dirtying the component
        /// </summary>
        protected virtual void AfterShowAlert() { }

        /// <summary>
        /// Invoked after clearing an alert prior to dirtying the component
        /// </summary>
        protected virtual void AfterClearAlert() { }
    }

    [Serializable, NetSerializable]
    public class AlertsComponentState : ComponentState
    {
        public Dictionary<AlertKey, AlertState> Alerts;

        public AlertsComponentState(Dictionary<AlertKey, AlertState> alerts)
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
        public readonly AlertType Type;

        public ClickAlertMessage(AlertType alertType)
        {
            Directed = true;
            Type = alertType;
        }
    }

    [Serializable, NetSerializable]
    public struct AlertState
    {
        public short? Severity;
        public (TimeSpan, TimeSpan)? Cooldown;
        public AlertType Type;
    }
}
