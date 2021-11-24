using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Alert;

public class SharedAlertsSystem : EntitySystem
{
    [Dependency]
    private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<AlertType, AlertPrototype> _typeToAlert = new();

    public override void Initialize()
    {
        base.Initialize();

        LoadAlertPrototypes();
    }

    /// <returns>true iff an alert of the indicated id is currently showing</returns>
    public static bool IsShowingAlert(SharedAlertsComponent sharedAlertsComponent, AlertType alertType)
    {
        if (TryGet(alertType, out var alert))
        {
            return sharedAlertsComponent.Alerts.ContainsKey(alert.AlertKey);
        }
        Logger.DebugS("alert", "unknown alert type {0}", alertType);
        return false;
    }

    /// <returns>true iff an alert of the indicated alert category is currently showing</returns>
    public static bool IsShowingAlertCategory(SharedAlertsComponent sharedAlertsComponent, AlertCategory alertCategory)
    {
        return sharedAlertsComponent.Alerts.ContainsKey(AlertKey.ForCategory(alertCategory));
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
        if (TryGet(alertType, out var alert))
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
        if (TryGet(alertType, out var alert))
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

    public static void LoadAlertPrototypes()
    {
        Get<SharedAlertsSystem>()._typeToAlert.Clear();

        foreach (var alert in Get<SharedAlertsSystem>()._prototypeManager.EnumeratePrototypes<AlertPrototype>())
        {
            if (!Get<SharedAlertsSystem>()._typeToAlert.TryAdd(alert.AlertType, alert))
            {
                Logger.ErrorS("alert",
                    "Found alert with duplicate alertType {0} - all alerts must have" +
                    " a unique alerttype, this one will be skipped", alert.AlertType);
            }
        }
    }

    /// <summary>
    /// Tries to get the alert of the indicated type
    /// </summary>
    /// <returns>true if found</returns>
    public static bool TryGet(AlertType alertType, [NotNullWhen(true)] out AlertPrototype? alert)
    {
        return Get<SharedAlertsSystem>()._typeToAlert.TryGetValue(alertType, out alert);
    }
}
