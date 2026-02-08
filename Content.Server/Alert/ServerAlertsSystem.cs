using Content.Shared.Alert;
using Robust.Shared.GameStates;

namespace Content.Server.Alert;

internal sealed class ServerAlertsSystem : AlertsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<AlertsComponent> alerts, ref ComponentGetState args)
    {
        // Relay: build display alerts for owner, optionally including relay-source alerts.
        var display = new Dictionary<AlertKey, AlertState>(alerts.Comp.Alerts);

        if (TryComp<AlertsDisplayRelayComponent>(alerts.Owner, out var relay) && relay.Source is { } src &&
            TryComp<AlertsComponent>(src, out var srcAlerts))
        {
            foreach (var (key, state) in srcAlerts.Alerts)
                display[key] = state;
        }

        // TODO: Use sourcegen when clone-state bug fixed.
        args.State = new AlertComponentState(display);
    }
}
