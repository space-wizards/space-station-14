using Content.Client.Alerts;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Shared.Alert;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Alerts;

public sealed class AlertsUIController : UIController, IOnStateEntered<GameplayState>
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
            UI?.SyncControls(system, system.AlertOrder, e);
    }

    public override void OnSystemLoaded(IEntitySystem system)
    {
        switch (system)
        {
            case ClientAlertsSystem alerts:
                BindToSystem(alerts);
                break;
        }
    }

    public override void OnSystemUnloaded(IEntitySystem system)
    {
        switch (system)
        {
            case ClientAlertsSystem:
                UnbindFromSystem();
                break;
        }
    }

    private void BindToSystem(ClientAlertsSystem system)
    {
        system.SyncAlerts += SystemOnSyncAlerts;
        system.ClearAlerts += SystemOnClearAlerts;
    }

    private void UnbindFromSystem()
    {
        if (_alertsSystem is null)
            throw new InvalidOperationException();

        _alertsSystem.SyncAlerts -= SystemOnSyncAlerts;
        _alertsSystem.ClearAlerts -= SystemOnClearAlerts;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (UI != null)
        {
            UI.AlertPressed += OnAlertPressed;
        }

        // initially populate the frame if system is available
        var alerts = _alertsSystem?.ActiveAlerts;
        if (alerts != null)
        {
            SystemOnSyncAlerts(_alertsSystem, alerts);
        }
    }
}
