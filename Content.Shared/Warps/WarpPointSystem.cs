using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Shared.Warps;

public sealed class WarpPointSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarpPointComponent, ExaminedEvent>(OnWarpPointExamine);
        SubscribeLocalEvent<WarpPointComponent, MapInitEvent>(OnStartUp);
    }

    private void OnStartUp(Entity<WarpPointComponent> ent, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(ent.Comp.Location) && Loc.TryGetString(ent.Comp.Location, out var locloc))
            ent.Comp.Location = locloc;
    }

    private void OnWarpPointExamine(EntityUid uid, WarpPointComponent component, ExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner) || string.IsNullOrEmpty(component.Location))
            return;

        args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", component.Location)));
    }
}
