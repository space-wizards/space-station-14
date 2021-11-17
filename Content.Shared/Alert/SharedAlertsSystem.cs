using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Shared.Alert;

public class SharedAlertsSystem : EntitySystem
{
    [Dependency] protected readonly AlertManager AlertManager = default!;

    /// <returns>true iff an alert of the indicated id is currently showing</returns>
    public bool IsShowingAlert(SharedAlertsComponent sharedAlertsComponent, AlertType alertType)
    {
        if (AlertManager.TryGet(alertType, out var alert))
        {
            return IsShowingAlert(sharedAlertsComponent, alert.AlertKey);
        }
        Logger.DebugS("alert", "unknown alert type {0}", alertType);
        return false;
    }

    /// <returns>true iff an alert of the indicated key is currently showing</returns>
    protected static bool IsShowingAlert(SharedAlertsComponent sharedAlertsComponent, AlertKey alertKey)
    {
        return sharedAlertsComponent.Alerts.ContainsKey(alertKey);
    }

    protected static IEnumerable<KeyValuePair<AlertKey, AlertState>> EnumerateAlertStates(SharedAlertsComponent sharedAlertsComponent)
    {
        return sharedAlertsComponent.Alerts;
    }

    /// <returns>true iff an alert of the indicated alert category is currently showing</returns>
    public static bool IsShowingAlertCategory(SharedAlertsComponent sharedAlertsComponent, AlertCategory alertCategory)
    {
        return SharedAlertsSystem.IsShowingAlert(sharedAlertsComponent, AlertKey.ForCategory(alertCategory));
    }

    public static bool TryGetAlertState(SharedAlertsComponent sharedAlertsComponent, AlertKey key, out AlertState alertState)
    {
        return sharedAlertsComponent.Alerts.TryGetValue(key, out alertState);
    }

    /// <summary>
    /// Shows the alert. If the alert or another alert of the same category is already showing,
    /// it will be updated / replaced with the specified values.
    /// </summary>
    /// <param name="sharedAlertsComponent"></param>
    /// <param name="alertType">type of the alert to set</param>
    /// <param name="severity">severity, if supported by the alert</param>
    /// <param name="cooldown">cooldown start and end, if null there will be no cooldown (and it will
    /// be erased if there is currently a cooldown for the alert)</param>
    public static void ShowAlert(SharedAlertsComponent sharedAlertsComponent, AlertType alertType, short? severity = null, (TimeSpan, TimeSpan)? cooldown = null)
    {
        var sys = EntitySystem.Get<SharedAlertsSystem>();
        if (sys.AlertManager.TryGet(alertType, out var alert))
        {
            // Check whether the alert category we want to show is already being displayed, with the same type,
            // severity, and cooldown.
            if (sharedAlertsComponent.Alerts.TryGetValue(alert.AlertKey, out var alertStateCallback) &&
                alertStateCallback.Type == alertType &&
                alertStateCallback.Severity == severity &&
                alertStateCallback.Cooldown == cooldown)
            {
                return;
            }

            // In the case we're changing the alert type but not the category, we need to remove it first.
            sharedAlertsComponent.Alerts.Remove(alert.AlertKey);

            sharedAlertsComponent.Alerts[alert.AlertKey] = new AlertState
                {Cooldown = cooldown, Severity = severity, Type=alertType};

            sys.AfterShowAlert(sharedAlertsComponent);

            sharedAlertsComponent.Dirty();

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
    public void ClearAlertCategory(SharedAlertsComponent sharedAlertsComponent, AlertCategory category)
    {
        var key = AlertKey.ForCategory(category);
        if (!sharedAlertsComponent.Alerts.Remove(key))
        {
            return;
        }

        AfterClearAlert(sharedAlertsComponent);

        sharedAlertsComponent.Dirty();
    }

    /// <summary>
    /// Clear the alert of the given type if it is currently showing.
    /// </summary>
    public void ClearAlert(SharedAlertsComponent sharedAlertsComponent, AlertType alertType)
    {
        if (AlertManager.TryGet(alertType, out var alert))
        {
            if (!sharedAlertsComponent.Alerts.Remove(alert.AlertKey))
            {
                return;
            }

            AfterClearAlert(sharedAlertsComponent);

            sharedAlertsComponent.Dirty();
        }
        else
        {
            Logger.ErrorS("alert", "unable to clear alert, unknown alertType {0}", alertType);
        }

    }

    /// <summary>
    /// Invoked after showing an alert prior to dirtying the component
    /// </summary>
    /// <param name="sharedAlertsComponent"></param>
    protected virtual void AfterShowAlert(SharedAlertsComponent sharedAlertsComponent) { }

    /// <summary>
    /// Invoked after clearing an alert prior to dirtying the component
    /// </summary>
    /// <param name="sharedAlertsComponent"></param>
    protected virtual void AfterClearAlert(SharedAlertsComponent sharedAlertsComponent) { }
}
