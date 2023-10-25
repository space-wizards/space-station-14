using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Content.Shared.NukeOps;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Content.Client.Antag;



namespace Content.Client.Overlays;
public sealed class ShowSyndicateIconsSystem : AntagStatusIconSystem<NukeOperativeComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeOperativeComponent, GetStatusIconsEvent>(GetNukeOpsIcon);
    }

    private void GetNukeOpsIcon(EntityUid uid, NukeOperativeComponent comp, ref GetStatusIconsEvent args)
    {
        GetStatusIcon(comp.SyndStatusIcon, ref args);
    }
}

