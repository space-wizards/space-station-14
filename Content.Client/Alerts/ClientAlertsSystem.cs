using System.Linq;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Alerts;

[UsedImplicitly]
public sealed class ClientAlertsSystem : AlertsSystem
{
    public AlertOrderPrototype? AlertOrder { get; set; }

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClientGameStateManager _gameState = default!;

    /// <summary>
    /// Syncing alerts can create entities, so we need to delay it, so it happens during an update.
    /// </summary>
    private SyncAlertAction _updateAlertActionOnNextTick = SyncAlertAction.None;

    public event EventHandler? ClearAlerts;
    public event EventHandler<IReadOnlyDictionary<AlertKey, AlertState>>? SyncAlerts;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<AlertsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<AlertsComponent, AfterAutoHandleStateEvent>(ClientAlertsHandleState);
        UpdatesOutsidePrediction = true;
    }
    protected override void LoadPrototypes()
    {
        base.LoadPrototypes();

        AlertOrder = _prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();
        if (AlertOrder == null)
            Log.Error("No alertOrder prototype found, alerts will be in random order");
    }

    public override void Update(float frameTime)
    {
        switch (_updateAlertActionOnNextTick)
        {
            case SyncAlertAction.Clear:
                ClearAlerts?.Invoke(this, EventArgs.Empty);
                _updateAlertActionOnNextTick = SyncAlertAction.None;
                break;
            case SyncAlertAction.Sync:
                if (_entityManager.TryGetComponent<AlertsComponent>(_playerManager.LocalEntity, out var alertComp))
                {
                    SyncAlerts?.Invoke(this, alertComp.Alerts);
                }
                _updateAlertActionOnNextTick = SyncAlertAction.None;
                break;
            case SyncAlertAction.None:
            default:
                break;
        }

        if (_gameState.IsPredictionEnabled)
        {
            base.Update(frameTime);
        }
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
        QueueAlertSync(alerts);
    }

    protected override void AfterClearAlert(Entity<AlertsComponent> alerts)
    {
        QueueAlertSync(alerts);
    }

    private void ClientAlertsHandleState(Entity<AlertsComponent> alerts, ref AfterAutoHandleStateEvent args)
    {
        QueueAlertSync(alerts);
    }

    private void QueueAlertSync(Entity<AlertsComponent> entity)
    {
        if (_playerManager.LocalEntity != entity.Owner)
            return;
        _updateAlertActionOnNextTick = SyncAlertAction.Sync;
    }

    private void OnPlayerAttached(EntityUid uid, AlertsComponent component, LocalPlayerAttachedEvent args)
    {
        _updateAlertActionOnNextTick = SyncAlertAction.Sync;
    }

    protected override void HandleComponentShutdown(EntityUid uid, AlertsComponent component, ComponentShutdown args)
    {
        base.HandleComponentShutdown(uid, component, args);

        if (_playerManager.LocalEntity != uid)
            return;
        _updateAlertActionOnNextTick = SyncAlertAction.Clear;
    }

    private void OnPlayerDetached(EntityUid uid, AlertsComponent component, LocalPlayerDetachedEvent args)
    {
        _updateAlertActionOnNextTick = SyncAlertAction.Clear;
    }

    public void AlertClicked(AlertType alertType)
    {
        RaiseNetworkEvent(new ClickAlertEvent(alertType));
    }

    private enum SyncAlertAction
    {
        None,
        Clear,
        Sync
    }
}
