using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Alert;

public abstract class AlertsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private FrozenDictionary<AlertType, AlertPrototype> _typeToAlert = default!;

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

        Log.Debug("Unknown alert type {0}", alertType);
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
    /// <param name="autoRemove">if true, the alert will be removed at the end of the cooldown</param>
    /// <param name="showCooldown">if true, the cooldown will be visibly shown over the alert icon</param>
    public void ShowAlert(EntityUid euid, AlertType alertType, short? severity = null, (TimeSpan, TimeSpan)? cooldown = null, bool autoRemove = false, bool showCooldown = true )
    {
        // This should be handled as part of networking.
        if (_timing.ApplyingState)
            return;

        if (!TryComp(euid, out AlertsComponent? alertsComponent))
            return;

        if (TryGet(alertType, out var alert))
        {
            // Check whether the alert category we want to show is already being displayed, with the same type,
            // severity, and cooldown.
            if (alertsComponent.Alerts.TryGetValue(alert.AlertKey, out var alertStateCallback) &&
                alertStateCallback.Type == alertType &&
                alertStateCallback.Severity == severity &&
                alertStateCallback.Cooldown == cooldown &&
                alertStateCallback.AutoRemove == autoRemove &&
                alertStateCallback.ShowCooldown == showCooldown)
            {
                return;
            }

            // In the case we're changing the alert type but not the category, we need to remove it first.
            alertsComponent.Alerts.Remove(alert.AlertKey);

            var state = new AlertState
                { Cooldown = cooldown, Severity = severity, Type = alertType, AutoRemove = autoRemove, ShowCooldown = showCooldown};
            alertsComponent.Alerts[alert.AlertKey] = state;

            // Keeping a list of AutoRemove alerts, so Update() doesn't need to check every alert
            if (autoRemove)
            {
                var autoComp = EnsureComp<AlertAutoRemoveComponent>(euid);
                if (!autoComp.AlertKeys.Contains(alert.AlertKey))
                    autoComp.AlertKeys.Add(alert.AlertKey);
            }

            AfterShowAlert((euid, alertsComponent));

            Dirty(euid, alertsComponent);
        }
        else
        {
            Log.Error("Unable to show alert {0}, please ensure this alertType has" +
                                   " a corresponding YML alert prototype",
                alertType);
        }
    }

    /// <summary>
    /// Clear the alert with the given category, if one is currently showing.
    /// </summary>
    public void ClearAlertCategory(EntityUid euid, AlertCategory category)
    {
        if(!TryComp(euid, out AlertsComponent? alertsComponent))
            return;

        var key = AlertKey.ForCategory(category);
        if (!alertsComponent.Alerts.Remove(key))
        {
            return;
        }

        AfterClearAlert((euid, alertsComponent));

        Dirty(euid, alertsComponent);
    }

    /// <summary>
    /// Clear the alert of the given type if it is currently showing.
    /// </summary>
    public void ClearAlert(EntityUid euid, AlertType alertType)
    {
        if (_timing.ApplyingState)
            return;

        if (!EntityManager.TryGetComponent(euid, out AlertsComponent? alertsComponent))
            return;

        if (TryGet(alertType, out var alert))
        {
            if (!alertsComponent.Alerts.Remove(alert.AlertKey))
            {
                return;
            }

            AfterClearAlert((euid, alertsComponent));

            Dirty(euid, alertsComponent);
        }
        else
        {
            Log.Error("Unable to clear alert, unknown alertType {0}", alertType);
        }
    }

    /// <summary>
    /// Invoked after showing an alert prior to dirtying the component
    /// </summary>
    protected virtual void AfterShowAlert(Entity<AlertsComponent> alerts) { }

    /// <summary>
    /// Invoked after clearing an alert prior to dirtying the component
    /// </summary>
    protected virtual void AfterClearAlert(Entity<AlertsComponent> alerts) { }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, ComponentStartup>(HandleComponentStartup);
        SubscribeLocalEvent<AlertsComponent, ComponentShutdown>(HandleComponentShutdown);
        SubscribeLocalEvent<AlertsComponent, PlayerAttachedEvent>(OnPlayerAttached);

        SubscribeLocalEvent<AlertAutoRemoveComponent, EntityUnpausedEvent>(OnAutoRemoveUnPaused);

        SubscribeNetworkEvent<ClickAlertEvent>(HandleClickAlert);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(HandlePrototypesReloaded);
        LoadPrototypes();
    }

    private void OnAutoRemoveUnPaused(EntityUid uid, AlertAutoRemoveComponent comp, EntityUnpausedEvent args)
    {
        if (!TryComp<AlertsComponent>(uid, out var alertComp))
        {
            return;
        }

        var dirty = false;

        foreach (var alert in alertComp.Alerts)
        {
            if (alert.Value.Cooldown is null)
                continue;

            var cooldown = (alert.Value.Cooldown.Value.Item1, alert.Value.Cooldown.Value.Item2 + args.PausedTime);

            var state = new AlertState
            {
                Severity = alert.Value.Severity,
                Cooldown = cooldown,
                ShowCooldown = alert.Value.ShowCooldown,
                AutoRemove = alert.Value.AutoRemove,
                Type = alert.Value.Type
            };
            alertComp.Alerts[alert.Key] = state;
            dirty = true;
        }

        if (dirty)
            Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AlertAutoRemoveComponent>();
        while (query.MoveNext(out var uid, out var autoComp))
        {
            var dirtyComp = false;
            if (autoComp.AlertKeys.Count <= 0 || !TryComp<AlertsComponent>(uid, out var alertComp))
            {
                RemCompDeferred(uid, autoComp);
                continue;
            }

            var removeList = new List<AlertKey>();
            foreach (var alertKey in autoComp.AlertKeys)
            {
                alertComp.Alerts.TryGetValue(alertKey, out var alertState);

                if (alertState.Cooldown is null || alertState.Cooldown.Value.Item2 >= _timing.CurTime)
                    continue;
                removeList.Add(alertKey);
                alertComp.Alerts.Remove(alertKey);
                dirtyComp = true;
            }

            foreach (var alertKey in removeList)
            {
                autoComp.AlertKeys.Remove(alertKey);
            }

            if (dirtyComp)
                Dirty(uid, alertComp);
        }
    }

    protected virtual void HandleComponentShutdown(EntityUid uid, AlertsComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(uid, new AlertSyncEvent(uid), true);
    }

    private void HandleComponentStartup(EntityUid uid, AlertsComponent component, ComponentStartup args)
    {
        RaiseLocalEvent(uid, new AlertSyncEvent(uid), true);
    }

    private void HandlePrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<AlertPrototype>())
            LoadPrototypes();
    }

    protected virtual void LoadPrototypes()
    {
        var dict = new Dictionary<AlertType, AlertPrototype>();
        foreach (var alert in _prototypeManager.EnumeratePrototypes<AlertPrototype>())
        {
            if (!dict.TryAdd(alert.AlertType, alert))
            {
                Log.Error("Found alert with duplicate alertType {0} - all alerts must have" +
                          " a unique alertType, this one will be skipped", alert.AlertType);
            }
        }

        _typeToAlert = dict.ToFrozenDictionary();
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
            Log.Debug("User {0} attempted to" +
                                   " click alert {1} which is not currently showing for them",
                EntityManager.GetComponent<MetaDataComponent>(player.Value).EntityName, msg.Type);
            return;
        }

        if (!TryGet(msg.Type, out var alert))
        {
            Log.Warning("Unrecognized encoded alert {0}", msg.Type);
            return;
        }

        alert.OnClick?.AlertClicked(player.Value);
    }

    private void OnPlayerAttached(EntityUid uid, AlertsComponent component, PlayerAttachedEvent args)
    {
        Dirty(uid, component);
    }
}
