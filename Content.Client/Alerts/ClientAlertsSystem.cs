using System.Linq;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Alerts;

[UsedImplicitly]
public sealed class ClientAlertsSystem : AlertsSystem
{
    public AlertOrderPrototype? AlertOrder { get; set; }

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public event EventHandler? ClearAlerts;
    public event EventHandler<IReadOnlyDictionary<AlertKey, AlertState>>? SyncAlerts;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<AlertsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<AlertsComponent, AfterAutoHandleStateEvent>(ClientAlertsHandleState);
    }
    protected override void LoadPrototypes()
    {
        base.LoadPrototypes();

        AlertOrder = _prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();
        if (AlertOrder == null)
            Log.Error("alert", "no alertOrder prototype found, alerts will be in random order");
    }

    public IReadOnlyDictionary<AlertKey, AlertState>? ActiveAlerts
    {
        get
        {
            var ent = _playerManager.LocalEntity;
            return ent is not null
                ? GetActiveAlerts(ent.Value)
                : null;
        }
    }

    protected override void AfterShowAlert(Entity<AlertsComponent> alerts)
    {
        UpdateHud(alerts);
    }

    protected override void AfterClearAlert(Entity<AlertsComponent> alerts)
    {
        UpdateHud(alerts);
    }

    private void ClientAlertsHandleState(Entity<AlertsComponent> alerts, ref AfterAutoHandleStateEvent args)
    {
        UpdateHud(alerts);
    }

    private void UpdateHud(Entity<AlertsComponent> entity)
    {
        if (_playerManager.LocalEntity == entity.Owner)
            SyncAlerts?.Invoke(this, entity.Comp.Alerts);
    }

    private void OnPlayerAttached(EntityUid uid, AlertsComponent component, LocalPlayerAttachedEvent args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        SyncAlerts?.Invoke(this, component.Alerts);
    }

    protected override void HandleComponentShutdown(EntityUid uid, AlertsComponent component, ComponentShutdown args)
    {
        base.HandleComponentShutdown(uid, component, args);

        if (_playerManager.LocalEntity != uid)
            return;

        ClearAlerts?.Invoke(this, EventArgs.Empty);
    }

    private void OnPlayerDetached(EntityUid uid, AlertsComponent component, LocalPlayerDetachedEvent args)
    {
        ClearAlerts?.Invoke(this, EventArgs.Empty);
    }

    public void AlertClicked(AlertType alertType)
    {
        RaiseNetworkEvent(new ClickAlertEvent(alertType));
    }
}
