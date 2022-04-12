using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Warps;

public sealed class WarpPointSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarpPointComponent, ExaminedEvent>(OnWarpPointExamine);
    }

    private static void OnWarpPointExamine(EntityUid uid, WarpPointComponent component, ExaminedEvent args)
    {
        var loc = component.Location == null ? "<null>" : $"'{component.Location}'";
        args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", loc)));
    }
}
