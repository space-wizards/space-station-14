using Content.Shared.Alert;
using Robust.Server.GameObjects;

namespace Content.Server.Alert;

internal sealed class ServerAlertsSystem : AlertsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertsComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(EntityUid uid, AlertsComponent component, PlayerAttachedEvent args)
    {
        Dirty(component);
    }
}
