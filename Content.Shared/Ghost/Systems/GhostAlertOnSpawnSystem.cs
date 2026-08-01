using Content.Shared.Alert;
using Content.Shared.Ghost.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Systems;

public sealed partial class GhostAlertOnSpawnSystem : EntitySystem
{
    [Dependency] private GhostAlertSystem _ghostAlert = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostAlertOnSpawnComponent, MapInitEvent>(OnSpawnInit);
    }

    private void OnSpawnInit(Entity<GhostAlertOnSpawnComponent> ent, ref MapInitEvent args)
    {
        _ghostAlert.MakeGhostAlert(ent, ent.Comp.Alert, ent.Comp.Cooldown);
    }
}
