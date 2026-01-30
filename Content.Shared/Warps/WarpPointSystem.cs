using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Shared.Warps;

public sealed class WarpPointSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarpPointComponent, ExaminedEvent>(OnWarpPointExamine);
        SubscribeLocalEvent<WarpPointComponent, ComponentStartup>(OnStartUp);
    }

    private void OnStartUp(Entity<WarpPointComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Location ??= Loc.GetString(ent.Comp.Location);
    }

    private void OnWarpPointExamine(EntityUid uid, WarpPointComponent component, ExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner) || string.IsNullOrEmpty(component.Location))
            return;

        args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", component.Location)));
    }
}
