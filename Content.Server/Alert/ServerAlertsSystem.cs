using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Alert;

internal sealed class ServerAlertsSystem : AlertsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, ComponentGetState>(OnGetState);
    }

    public override void ShowAlert(
        EntityUid euid,
        ProtoId<AlertPrototype> alertType,
        short? severity = null,
        (TimeSpan, TimeSpan)? cooldown = null,
        bool autoRemove = false,
        bool showCooldown = true)
    {
#if DEBUG
        if (!TryGet(alertType, out var alert))
            return;

        DebugTools.Assert(!alert.ClientOnly, "Tried to set a client-only alert on the server.");
#endif

        base.ShowAlert(euid, alertType, severity, cooldown, autoRemove, showCooldown);
    }

    private void OnGetState(Entity<AlertsComponent> alerts, ref ComponentGetState args)
    {
        // TODO: Use sourcegen when clone-state bug fixed.
        args.State = new AlertComponentState(new(alerts.Comp.Alerts));
    }
}
