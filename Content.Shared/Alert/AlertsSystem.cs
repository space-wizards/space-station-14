using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Alert;

public abstract class AlertsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private EntityQuery<AlertsComponent> _alertsQuery;
    private FrozenDictionary<ProtoId<AlertPrototype>, AlertPrototype> _typeToAlert = default!;

    public override void Initialize()
    {
        base.Initialize();

        _alertsQuery = GetEntityQuery<AlertsComponent>();

        SubscribeLocalEvent<AlertsComponent, ComponentStartup>(HandleComponentStartup);
        SubscribeLocalEvent<AlertsComponent, ComponentShutdown>(HandleComponentShutdown);
        SubscribeLocalEvent<AlertsComponent, PlayerAttachedEvent>(OnPlayerAttached);

        SubscribeLocalEvent<AlertAutoRemoveComponent, EntityUnpausedEvent>(OnAutoRemoveUnPaused);

        SubscribeAllEvent<ClickAlertEvent>(HandleClickAlert);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(HandlePrototypesReloaded);
        LoadPrototypes();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AlertAutoRemoveComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var autoComp))
        {
            var removed = false;
            if (autoComp.AlertKeys.Count <= 0 || !_alertsQuery.TryComp(uid, out var alertComp))
            {
                RemCompDeferred(uid, autoComp);
                continue;
            }

            var removeList = new List<AlertKey>();
            foreach (var alertKey in autoComp.AlertKeys)
            {
                alertComp.Alerts.TryGetValue(alertKey, out var alertState);

                if (alertState.Cooldown is null || alertState.Cooldown.Value.endTime >= curTime)
                    continue;

                removeList.Add(alertKey);
                alertComp.Alerts.Remove(alertKey);
                removed = true;
            }

            if (!removed)
                continue;

            foreach (var alertKey in removeList)
            {
                autoComp.AlertKeys.Remove(alertKey);
            }

            Dirty(uid, alertComp);
            Dirty(uid, autoComp);
        }
    }

    public IReadOnlyDictionary<AlertKey, AlertState>? GetActiveAlerts(Entity<AlertsComponent?> entity)
    {
        return _alertsQuery.Resolve(entity, ref entity.Comp, false)
            ? entity.Comp.Alerts
            : null;
    }

    public short GetSeverityRange(ProtoId<AlertPrototype> alertType)
    {
        var minSeverity = _typeToAlert[alertType].MinSeverity;
        return (short)MathF.Max(minSeverity, _typeToAlert[alertType].MaxSeverity - minSeverity);
    }

    public short GetMaxSeverity(ProtoId<AlertPrototype> alertType)
    {
        return _typeToAlert[alertType].MaxSeverity;
    }

    public short GetMinSeverity(ProtoId<AlertPrototype> alertType)
    {
        return _typeToAlert[alertType].MinSeverity;
    }

    public bool IsShowingAlert(Entity<AlertsComponent?> entity, ProtoId<AlertPrototype> alertType)
    {
        if (!_alertsQuery.Resolve(entity, ref entity.Comp, false))
            return false;

        if (TryGet(alertType, out var alert))
            return entity.Comp.Alerts.ContainsKey(alert.AlertKey);

        Log.Debug($"Unknown alert type {alertType}");
        return false;
    }

    /// <returns>true iff an alert of the indicated alert category is currently showing</returns>
    public bool IsShowingAlertCategory(Entity<AlertsComponent?> entity, ProtoId<AlertCategoryPrototype> alertCategory)
    {
        return _alertsQuery.Resolve(entity, ref entity.Comp, false)
               && entity.Comp.Alerts.ContainsKey(AlertKey.ForCategory(alertCategory));
    }

    public bool TryGetAlertState(Entity<AlertsComponent?> entity, AlertKey key, out AlertState alertState)
    {
        if (_alertsQuery.Resolve(entity, ref entity.Comp, false))
            return entity.Comp.Alerts.TryGetValue(key, out alertState);

        alertState = default;
        return false;

    }

    /// <summary>
    /// Shows the alert. If the alert or another alert of the same category is already showing,
    /// it will be updated / replaced with the specified values.
    /// </summary>
    /// <param name="entity">The entity who we are showing the alert for.</param>
    /// <param name="alertType">type of the alert to set</param>
    /// <param name="severity">severity, if supported by the alert</param>
    /// <param name="cooldown">cooldown start and end, if null there will be no cooldown (and it will
    ///     be erased if there is currently a cooldown for the alert)</param>
    /// <param name="autoRemove">if true, the alert will be removed at the end of the cooldown</param>
    /// <param name="showCooldown">if true, the cooldown will be visibly shown over the alert icon</param>
    public void ShowAlert(Entity<AlertsComponent?> entity,
        ProtoId<AlertPrototype> alertType,
        short? severity = null,
        (TimeSpan, TimeSpan)? cooldown = null,
        bool autoRemove = false,
        bool showCooldown = true )
    {
        ShowAlert(entity, new AlertState { Type = alertType, Severity = severity, Cooldown = cooldown, AutoRemove = autoRemove, ShowCooldown = showCooldown});
    }

    public void ShowAlert(Entity<AlertsComponent?> entity, AlertState state)
    {
        // This should be handled as part of networking.
        if (_timing.ApplyingState)
            return;

        if (!_alertsQuery.Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryGet(state.Type, out var alert))
        {
            Log.Error($"Unable to show alert {state.Type}, please ensure this alertType has a corresponding YML alert prototype");
            return;
        }

        // Check whether the alert category we want to show is already being displayed, with the same type,
        // severity, and cooldown.
        if (entity.Comp.Alerts.TryGetValue(alert.AlertKey, out var alertStateCallback))
        {
            if (state == alertStateCallback)
                return;

            // If the alert exists and we're updating it, we need to remove it first before adding it back.
            entity.Comp.Alerts.Remove(alert.AlertKey);
        }

        entity.Comp.Alerts.Add(alert.AlertKey, state);

        // Keeping a list of AutoRemove alerts, so Update() doesn't need to check every alert
        if (state.AutoRemove)
        {
            EnsureComp<AlertAutoRemoveComponent>(entity, out var autoComp);

            if (autoComp.AlertKeys.Add(alert.AlertKey))
                Dirty (entity, autoComp);
        }

        AfterShowAlert((entity, entity.Comp));

        Dirty(entity);
    }

    /// <summary>
    /// An alternative to show alert with different behavior if an alert already exists.
    /// </summary>
    /// <param name="entity">Entity whose alert we're updating</param>
    /// <param name="alertType">Prototype of the alert we're updating</param>
    /// <param name="severity">Severity we're setting the alert to</param>
    /// <param name="cooldown">Time left in the alert.</param>
    /// <param name="autoRemove">Do we want to remove this alert when it expires?</param>
    /// <param name="showCooldown">Should we show/hide the cooldown?</param>
    public void UpdateAlert(Entity<AlertsComponent?> entity,
        ProtoId<AlertPrototype> alertType,
        short? severity = null,
        TimeSpan? cooldown = null,
        bool autoRemove = false,
        bool showCooldown = true)
    {
        if (_timing.ApplyingState)
            return;

        if (!_alertsQuery.Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryGet(alertType, out var alert))
            return;

        if (cooldown == null)
        {
            ShowAlert(entity, alertType, severity, null, autoRemove, showCooldown);
            return;
        }

        // Keep the progress duration the same but only if we're removing time.
        // If the next cooldown is greater than our previous one we should reset the timer
        TryGetAlertState(entity, alert.AlertKey, out var alertState);
        var down = alertState.Cooldown?.endTime < cooldown.Value
            ? (_timing.CurTime, cooldown.Value)
            : (alertState.Cooldown?.startTime ?? _timing.CurTime, cooldown.Value);

        ShowAlert(entity, alertType, severity, down, autoRemove, showCooldown);
    }

    /// <summary>
    /// Clear the alert with the given category, if one is currently showing.
    /// </summary>
    public void ClearAlertCategory(Entity<AlertsComponent?> entity, ProtoId<AlertCategoryPrototype> category)
    {
        if(!_alertsQuery.Resolve(entity, ref entity.Comp, false))
            return;

        var key = AlertKey.ForCategory(category);
        if (!entity.Comp.Alerts.Remove(key))
        {
            return;
        }

        AfterClearAlert((entity, entity.Comp));

        Dirty(entity);
    }

    /// <summary>
    /// Clear the alert of the given type if it is currently showing.
    /// </summary>
    public void ClearAlert(Entity<AlertsComponent?> entity, ProtoId<AlertPrototype> alertType)
    {
        if (_timing.ApplyingState)
            return;

        if (!_alertsQuery.Resolve(entity, ref entity.Comp, false))
            return;

        if (TryGet(alertType, out var alert))
        {
            if (!entity.Comp.Alerts.Remove(alert.AlertKey))
            {
                return;
            }

            AfterClearAlert((entity, entity.Comp));

            Dirty(entity);
        }
        else
        {
            Log.Error($"Unable to clear alert, unknown alertType {alertType}");
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

    private void OnAutoRemoveUnPaused(Entity<AlertAutoRemoveComponent> entity, ref EntityUnpausedEvent args)
    {
        if (!_alertsQuery.TryComp(entity, out var alertComp))
            return;

        var dirty = false;

        foreach (var alert in alertComp.Alerts)
        {
            if (alert.Value.Cooldown is null)
                continue;

            var (start, end) = alert.Value.Cooldown.Value;
            var cooldown = (start, end + args.PausedTime);

            var state = alert.Value with { Cooldown = cooldown };
            alertComp.Alerts[alert.Key] = state;
            dirty = true;
        }

        if (dirty)
            Dirty(entity, alertComp);
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
        var dict = new Dictionary<ProtoId<AlertPrototype>, AlertPrototype>();
        foreach (var alert in _prototypeManager.EnumeratePrototypes<AlertPrototype>())
        {
            if (!dict.TryAdd(alert.ID, alert))
                Log.Error($"Found alert with duplicate alertType {alert.ID} - all alerts must have a unique alertType, this one will be skipped");
        }

        _typeToAlert = dict.ToFrozenDictionary();
    }

    /// <summary>
    /// Tries to get the alert of the indicated type
    /// </summary>
    /// <returns>true if found</returns>
    public bool TryGet(ProtoId<AlertPrototype> alertType, [NotNullWhen(true)] out AlertPrototype? alert)
    {
        return _typeToAlert.TryGetValue(alertType, out alert);
    }

    private void HandleClickAlert(ClickAlertEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (player is null || !HasComp<AlertsComponent>(player))
            return;

        if (!IsShowingAlert(player.Value, msg.Type))
        {
            Log.Debug($"User {ToPrettyString(player.Value)} attempted to click alert {msg.Type} which is not currently showing for them");
            return;
        }

        if (!TryGet(msg.Type, out var alert))
        {
            Log.Warning($"Unrecognized encoded alert {msg.Type}");
            return;
        }

        if (ActivateAlert(player.Value, alert) && _timing.IsFirstTimePredicted)
        {
            HandledAlert();
        }
    }

    protected virtual void HandledAlert()
    {

    }

    public bool ActivateAlert(EntityUid user, AlertPrototype alert)
    {
        if (alert.ClickEvent is not { } clickEvent)
            return false;

        clickEvent.Handled = false;
        clickEvent.User = user;
        clickEvent.AlertId = alert.ID;

        RaiseLocalEvent(user, (object) clickEvent, true);
        return clickEvent.Handled;
    }

    private void OnPlayerAttached(EntityUid uid, AlertsComponent component, PlayerAttachedEvent args)
    {
        Dirty(uid, component);
    }
}
