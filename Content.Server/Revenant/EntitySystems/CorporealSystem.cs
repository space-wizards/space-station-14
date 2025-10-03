using Content.Server.Ghost;
using Content.Shared.Ghost;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.EntitySystems;

namespace Content.Server.Revenant.EntitySystems;

public sealed class CorporealSystem : SharedCorporealSystem
{
    [Dependency] private readonly GhostVisibilitySystem _ghostVis = default!;

    public override void OnStartup(EntityUid uid, CorporealComponent component, ComponentStartup args)
    {
        base.OnStartup(uid, component, args);
        var ghost = EnsureComp<GhostVisibilityComponent>(uid);
        _ghostVis.SetVisibleOverride((uid, ghost), true);
    }

    public override void OnShutdown(EntityUid uid, CorporealComponent component, ComponentShutdown args)
    {
        base.OnShutdown(uid, component, args);
        _ghostVis.SetVisibleOverride(uid, null);
    }
}
