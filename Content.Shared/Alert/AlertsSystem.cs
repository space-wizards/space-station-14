using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Alert;

public abstract class AlertsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly Dictionary<AlertType, AlertPrototype> _typeToAlert = new();

    public IReadOnlyDictionary<AlertKey, AlertState>? GetActiveAlerts(EntityUid euid)
    {
        return EntityManager.TryGetComponent(euid, out AlertsComponent? comp)
            ? comp.Alerts
            : null;
    }

    public short GetSeverityRange(AlertType alertType)
    {
        var minSeverity = _typeToAlert[alertType].MinSeverity;
        return (short)MathF.Max(minSeverity,_typeToAlert[alertType].MaxSeverity - minSeverity);
    }

    public short GetMaxSeverity(AlertType alertType)
    {
        return _typeToAlert[alertType].MaxSeverity;
    }

    public short GetMinSeverity(AlertType alertType)
    {
        return _typeToAlert[alertType].MinSeverity;
    }

    public bool IsShowingAlert(EntityUid euid, AlertType alertType)
    {
        if (!EntityManager.TryGetComponent(euid, out AlertsComponent? alertsComponent))
            return false;

        if (TryGet(alertType, out var alert))
        {
            return alertsComponent.Alerts.ContainsKey(alert.AlertKey);
        }

        Logger.DebugS("alert", "unknown alert type {0}", alertType);
        return false;
    }

    /// <returns>true iff an alert of the indicated alert category is currently showing</returns>
    public bool IsShowingAlertCategory(EntityUid euid, AlertCategory alertCategory)
    {
        return EntityManager.TryGetComponent(euid, out AlertsComponent? alertsComponent)
               && alertsComponent.Alerts.ContainsKey(AlertKey.ForCategory(alertCategory));
    }

    public bool TryGetAlertState(EntityUid euid, AlertKey key, out AlertState alertState)
    {
        if (EntityManager.TryGetComponent(euid, out AlertsComponent? alertsComponent))
            return alertsComponent.Alerts.TryGetValue(key, out alertState);

        alertState = default;
        return false;

    }

    /// <summary>
    /// Shows the alert. If the alert or another alert of the same category is already showing,
    /// it will be updated / replaced with the specified values.
    /// </summary>
    /// <param name="euid"></param>
    /// <param name="alertType">type of the alert to set</param>
    /// <param name="severity">severity, if supported by the alert</param>
    /// <param name="cooldown">cooldown start and end, if null there will be no cooldown (and it will
    ///     be erased if there is currently a cooldown for the alert)</param>
    public void ShowAlert(EntityUid euid, AlertType alertType, short? severity = null, (TimeSpan, TimeSpan)? cooldown = null)
    {
        if (!EntityManager.TryGetComponent(euid, out AlertsComponent? alertsComponent))
            return;

        if (TryGet(alertType, out var alert))
        {
            // Check whether the alert category we want to show is already being displayed, with the same type,
            // severity, and cooldown.
            if (alertsComponent.Alerts.TryGetValue(alert.AlertKey, out var alertStateCallback) &&
                alertStateCallback.Type == alertType &&
                alertStateCallback.Severity == severity &&
                alertStateCallback.Cooldown == cooldown)
            {
                return;
            }

            // In the case we're changing the alert type but not the category, we need to remove it first.
            alertsComponent.Alerts.Remove(alert.AlertKey);

            alertsComponent.Alerts[alert.AlertKey] = new AlertState
                { Cooldown = cooldown, Severity = severity, Type = alertType };

            AfterShowAlert(alertsComponent);

            Dirty(alertsComponent);
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
    public void ClearAlertCategory(EntityUid euid, AlertCategory category)
    {
        if(!EntityManager.TryGetComponent(euid, out AlertsComponent? alertsComponent))
            return;

        var key = AlertKey.ForCategory(category);
        if (!alertsComponent.Alerts.Remove(key))
        {
            return;
        }

        AfterClearAlert(alertsComponent);

        Dirty(alertsComponent);
    }

    /// <summary>
    /// Clear the alert of the given type if it is currently showing.
    /// </summary>
    public void ClearAlert(EntityUid euid, AlertType alertType)
    {
        if (!EntityManager.TryGetComponent(euid, out AlertsComponent? alertsComponent))
            return;

        if (TryGet(alertType, out var alert))
        {
            if (!alertsComponent.Alerts.Remove(alert.AlertKey))
            {
                return;
            }

            AfterClearAlert(alertsComponent);

            Dirty(alertsComponent);
        }
        else
        {
            Logger.ErrorS("alert", "unable to clear alert, unknown alertType {0}", alertType);
        }
    }

    /// <summary>
    /// Invoked after showing an alert prior to dirtying the component
    /// </summary>
    /// <param name="alertsComponent"></param>
    protected virtual void AfterShowAlert(AlertsComponent alertsComponent) { }

    /// <summary>
    /// Invoked after clearing an alert prior to dirtying the component
    /// </summary>
    /// <param name="alertsComponent"></param>
    protected virtual void AfterClearAlert(AlertsComponent alertsComponent) { }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, ComponentStartup>(HandleComponentStartup);
        SubscribeLocalEvent<AlertsComponent, ComponentShutdown>(HandleComponentShutdown);

        SubscribeLocalEvent<AlertsComponent, ComponentGetState>(ClientAlertsGetState);
        SubscribeNetworkEvent<ClickAlertEvent>(HandleClickAlert);

        LoadPrototypes();
        _prototypeManager.PrototypesReloaded += HandlePrototypesReloaded;
    }

    protected virtual void HandleComponentShutdown(EntityUid uid, AlertsComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(uid, new AlertSyncEvent(uid), true);
    }

    private void HandleComponentStartup(EntityUid uid, AlertsComponent component, ComponentStartup args)
    {
        RaiseLocalEvent(uid, new AlertSyncEvent(uid), true);
    }

    public override void Shutdown()
    {
        _prototypeManager.PrototypesReloaded -= HandlePrototypesReloaded;

        base.Shutdown();
    }

    private void HandlePrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        LoadPrototypes();
    }

    protected virtual void LoadPrototypes()
    {
        _typeToAlert.Clear();
        foreach (var alert in _prototypeManager.EnumeratePrototypes<AlertPrototype>())
        {
            if (!_typeToAlert.TryAdd(alert.AlertType, alert))
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
    public bool TryGet(AlertType alertType, [NotNullWhen(true)] out AlertPrototype? alert)
    {
        return _typeToAlert.TryGetValue(alertType, out alert);
    }

    private void HandleClickAlert(ClickAlertEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (player is null || !EntityManager.HasComponent<AlertsComponent>(player))
            return;

        if (!IsShowingAlert(player.Value, msg.Type))
        {
            Logger.DebugS("alert", "user {0} attempted to" +
                                   " click alert {1} which is not currently showing for them",
                EntityManager.GetComponent<MetaDataComponent>(player.Value).EntityName, msg.Type);
            return;
        }

        if (!TryGet(msg.Type, out var alert))
        {
            Logger.WarningS("alert", "unrecognized encoded alert {0}", msg.Type);
            return;
        }

        alert.OnClick?.AlertClicked(player.Value);
    }

    private static void ClientAlertsGetState(EntityUid uid, AlertsComponent component, ref ComponentGetState args)
    {
        args.State = new AlertsComponentState(component.Alerts);
    }
}
