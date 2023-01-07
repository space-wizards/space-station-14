using Content.Client.Alerts;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Shared.Alert;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Alerts;

public sealed class AlertsUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<ClientAlertsSystem>
{
    [UISystemDependency] private readonly ClientAlertsSystem? _alertsSystem = default;

    private AlertsUI? UI => UIManager.GetActiveUIWidgetOrNull<AlertsUI>();

    private void OnAlertPressed(object? sender, AlertType e)
    {
        _alertsSystem?.AlertClicked(e);
    }

    private void SystemOnClearAlerts(object? sender, EventArgs e)
    {
        UI?.ClearAllControls();
    }

    private void SystemOnSyncAlerts(object? sender, IReadOnlyDictionary<AlertKey, AlertState> e)
    {
        if (sender is ClientAlertsSystem system)
        {
            UI?.SyncControls(system, system.AlertOrder, e);
        }

        // The UI can change underneath us if the user switches between HUD layouts
        // So ensure we're subscribed to the AlertPressed callback.
        if (UI != null)
        {
            UI.AlertPressed -= OnAlertPressed; // Ensure we don't hook into the callback twice
            UI.AlertPressed += OnAlertPressed;
        }
    }

    public void OnSystemLoaded(ClientAlertsSystem system)
    {
        system.SyncAlerts += SystemOnSyncAlerts;
        system.ClearAlerts += SystemOnClearAlerts;
    }

    public void OnSystemUnloaded(ClientAlertsSystem system)
    {
        system.SyncAlerts -= SystemOnSyncAlerts;
        system.ClearAlerts -= SystemOnClearAlerts;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (UI != null)
        {
            UI.AlertPressed += OnAlertPressed;
        }

        // initially populate the frame if system is available
        SyncAlerts();
    }

    public void SyncAlerts()
    {
        var alerts = _alertsSystem?.ActiveAlerts;
        if (alerts != null)
        {
            SystemOnSyncAlerts(_alertsSystem, alerts);
        }
    }
}
