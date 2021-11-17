using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Alerts.UI;
using Content.Shared.Alert;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Client.Alerts;

internal class ClientAlertsSystem : SharedAlertsSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClientAlertsComponent, PlayerAttachedEvent>((_, component, _) => PlayerAttached(component));
        SubscribeLocalEvent<ClientAlertsComponent, PlayerDetachedEvent>((_, component, _) => PlayerDetached(component));

        SubscribeLocalEvent<ClientAlertsComponent, ComponentHandleState>(ClientAlertsHandleState);
        SubscribeLocalEvent<ClientAlertsComponent, ComponentShutdown>((_, component, _) => PlayerDetached(component));
    }

    protected override void AfterShowAlert(SharedAlertsComponent sharedAlertsComponent)
    {
        UpdateAlertsControls((ClientAlertsComponent) sharedAlertsComponent);
    }

    protected override void AfterClearAlert(SharedAlertsComponent sharedAlertsComponent)
    {
        UpdateAlertsControls((ClientAlertsComponent) sharedAlertsComponent);
    }

    private void ClientAlertsHandleState(EntityUid uid, ClientAlertsComponent component, ref ComponentHandleState args)
    {
        var componentAlerts = (args.Current as AlertsComponentState)?.Alerts;
        if (componentAlerts == null) return;

        UpdateAlertsControls(component);
        component.Alerts = componentAlerts;
    }

    /// <summary>
    ///     Updates the displayed alerts based on current state of Alerts, performing
    ///     a diff to ensure we only change what's changed (this avoids active tooltips disappearing any
    ///     time state changes)
    /// </summary>
    /// <param name="clientAlertsComponent"></param>
    private void UpdateAlertsControls(ClientAlertsComponent clientAlertsComponent)
    {
        if (!CurControlled(clientAlertsComponent.Owner, _playerManager) || clientAlertsComponent.AlertUi == null) return;

        // remove any controls with keys no longer present
        var toRemove = new List<AlertKey>();
        foreach (var existingKey in clientAlertsComponent.AlertControls.Keys)
        {
            if (!IsShowingAlert(clientAlertsComponent, existingKey)) toRemove.Add(existingKey);
        }

        foreach (var alertKeyToRemove in toRemove)
        {
            clientAlertsComponent.AlertControls.Remove(alertKeyToRemove, out var control);
            if (control == null) return;
            clientAlertsComponent.AlertUi.AlertContainer.Children.Remove(control);
        }

        // now we know that alertControls contains alerts that should still exist but
        // may need to updated,
        // also there may be some new alerts we need to show.
        // further, we need to ensure they are ordered w.r.t their configured order
        foreach (var (alertKey, alertState) in EnumerateAlertStates(clientAlertsComponent))
        {
            if (!alertKey.AlertType.HasValue)
            {
                Logger.WarningS("alert", "found alertkey without alerttype," +
                                         " alert keys should never be stored without an alerttype set: {0}", alertKey);
                continue;
            }

            var alertType = alertKey.AlertType.Value;
            if (!AlertManager.TryGet(alertType, out var newAlert))
            {
                Logger.ErrorS("alert", "Unrecognized alertType {0}", alertType);
                continue;
            }

            if (clientAlertsComponent.AlertControls.TryGetValue(newAlert.AlertKey, out var existingAlertControl) &&
                existingAlertControl.Alert.AlertType == newAlert.AlertType)
            {
                // key is the same, simply update the existing control severity / cooldown
                existingAlertControl.SetSeverity(alertState.Severity);
                existingAlertControl.Cooldown = alertState.Cooldown;
            }
            else
            {
                if (existingAlertControl != null) clientAlertsComponent.AlertUi.AlertContainer.Children.Remove(existingAlertControl);

                // this is a new alert + alert key or just a different alert with the same
                // key, create the control and add it in the appropriate order
                var newAlertControl = CreateAlertControl(newAlert, alertState);
                if (clientAlertsComponent.AlertOrder != null)
                {
                    var added = false;
                    foreach (var alertControl in clientAlertsComponent.AlertUi.AlertContainer.Children)
                    {
                        if (clientAlertsComponent.AlertOrder.Compare(newAlert, ((AlertControl) alertControl).Alert) < 0)
                        {
                            var idx = alertControl.GetPositionInParent();
                            clientAlertsComponent.AlertUi.AlertContainer.Children.Add(newAlertControl);
                            newAlertControl.SetPositionInParent(idx);
                            added = true;
                            break;
                        }
                    }

                    if (!added) clientAlertsComponent.AlertUi.AlertContainer.Children.Add(newAlertControl);
                }
                else
                    clientAlertsComponent.AlertUi.AlertContainer.Children.Add(newAlertControl);

                clientAlertsComponent.AlertControls[newAlert.AlertKey] = newAlertControl;
            }
        }
    }

    private void PlayerAttached(ClientAlertsComponent clientAlertsComponent)
    {
        if (!CurControlled(clientAlertsComponent.Owner, _playerManager) || clientAlertsComponent.AlertUi != null) return;

        clientAlertsComponent.AlertOrder = _prototypeManager.EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();
        if (clientAlertsComponent.AlertOrder == null)
            Logger.ErrorS("alert", "no alertOrder prototype found, alerts will be in random order");

        clientAlertsComponent.AlertUi = new AlertsUI();
        _userInterfaceManager.StateRoot.AddChild(clientAlertsComponent.AlertUi);

        UpdateAlertsControls(clientAlertsComponent);
    }

    private void PlayerDetached(ClientAlertsComponent clientAlertsComponent)
    {
        foreach (var alertControl in clientAlertsComponent.AlertControls.Values)
        {
            alertControl.OnPressed -= (Action<BaseButton.ButtonEventArgs>) AlertControlPressed;
        }

        if (clientAlertsComponent.AlertUi != null)
        {
            _userInterfaceManager.StateRoot.RemoveChild(clientAlertsComponent.AlertUi);
            clientAlertsComponent.AlertUi = null;
        }

        clientAlertsComponent.AlertControls.Clear();
    }

    private void AlertControlPressed(BaseButton.ButtonEventArgs args)
    {
        if (args.Button is not AlertControl control)
            return;

        if (args.Event.Function != EngineKeyFunctions.UIClick)
            return;

        RaiseNetworkEvent(new ClickAlertEvent(control.Alert.AlertType));
    }

    private AlertControl CreateAlertControl(AlertPrototype alert, AlertState alertState)
    {
        var alertControl = new AlertControl(alert, alertState.Severity)
        {
            Cooldown = alertState.Cooldown
        };
        alertControl.OnPressed += AlertControlPressed;
        return alertControl;
    }

    /// <summary>
    ///     Allows calculating if we need to act due to this component being controlled by the current mob
    /// </summary>
    private static bool CurControlled(IEntity entity, IPlayerManager playerManager)
    {
        return playerManager.LocalPlayer != null && playerManager.LocalPlayer.ControlledEntity == entity;
    }
}
