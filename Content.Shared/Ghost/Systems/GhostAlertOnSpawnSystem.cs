using Content.Shared.Ghost.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Shared.Ghost.Systems;

public sealed partial class GhostAlertOnSpawnSystem : EntitySystem
{
    [Dependency] private AlertTeleportSystem _alertTeleport = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GhostAlertOnSpawnComponent> ent, ref MapInitEvent args)
    {
        _alertTeleport.MakeTeleportAlert<GhostAlertsComponent>(ent, ent.Comp.Alert, ent.Comp.AlertDelay);
    }
}
