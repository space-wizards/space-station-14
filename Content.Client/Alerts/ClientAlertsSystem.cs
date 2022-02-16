using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Client.Alerts;

[UsedImplicitly]
internal sealed class ClientAlertsSystem : AlertsSystem
{
    public AlertOrderPrototype? AlertOrder { get; set; }

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public event EventHandler? ClearAlerts;
    public event EventHandler<IReadOnlyDictionary<AlertKey, AlertState>>? SyncAlerts;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, PlayerAttachedEvent>((_, component, _) => PlayerAttached(component));
        SubscribeLocalEvent<AlertsComponent, PlayerDetachedEvent>((_, _, _) => PlayerDetached());

        SubscribeLocalEvent<AlertsComponent, ComponentHandleState>(ClientAlertsHandleState);
    }

    protected override void LoadPrototypes()
    {
        base.LoadPrototypes();

        AlertOrder = _prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();
        if (AlertOrder == null)
            Logger.ErrorS("alert", "no alertOrder prototype found, alerts will be in random order");
    }

    public IReadOnlyDictionary<AlertKey, AlertState>? ActiveAlerts
    {
        get
        {
            var ent = _playerManager.LocalPlayer?.ControlledEntity;
            return ent is not null
                ? GetActiveAlerts(ent.Value)
                : null;
        }
    }

    protected override void AfterShowAlert(AlertsComponent alertsComponent)
    {
        if (!CurControlled(alertsComponent.Owner, _playerManager))
            return;

        SyncAlerts?.Invoke(this, alertsComponent.Alerts);
    }

    protected override void AfterClearAlert(AlertsComponent alertsComponent)
    {
        if (!CurControlled(alertsComponent.Owner, _playerManager))
            return;

        SyncAlerts?.Invoke(this, alertsComponent.Alerts);
    }

    private void ClientAlertsHandleState(EntityUid uid, AlertsComponent component, ref ComponentHandleState args)
    {
        var componentAlerts = (args.Current as AlertsComponentState)?.Alerts;
        if (componentAlerts == null) return;

        //TODO: Do we really want to send alerts for non-attached entity?
        component.Alerts = new(componentAlerts);
        if (!CurControlled(component.Owner, _playerManager)) return;

        SyncAlerts?.Invoke(this, componentAlerts);
    }

    private void PlayerAttached(AlertsComponent clientAlertsComponent)
    {
        if (!CurControlled(clientAlertsComponent.Owner, _playerManager)) return;
        SyncAlerts?.Invoke(this, clientAlertsComponent.Alerts);
    }

    protected override void HandleComponentShutdown(EntityUid uid)
    {
        base.HandleComponentShutdown(uid);

        PlayerDetached();
    }

    private void PlayerDetached()
    {
        ClearAlerts?.Invoke(this, EventArgs.Empty);
    }

    public void AlertClicked(AlertType alertType)
    {
        RaiseNetworkEvent(new ClickAlertEvent(alertType));
    }

    /// <summary>
    ///     Allows calculating if we need to act due to this component being controlled by the current mob
    /// </summary>
    private static bool CurControlled(EntityUid entity, IPlayerManager playerManager)
    {
        return playerManager.LocalPlayer != null && playerManager.LocalPlayer.ControlledEntity == entity;
    }
}
