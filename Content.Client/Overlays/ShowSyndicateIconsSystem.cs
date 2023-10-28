using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Content.Shared.NukeOps;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;
public sealed class ShowSyndicateIconsSystem : EquipmentHudSystem<ShowSyndicateIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeOperativeComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, NukeOperativeComponent nukeOperativeComponent, ref GetStatusIconsEvent args)
    {
        if (!IsActive || args.InContainer)
        {
            return;
        }

        var healthIcons = SyndicateIcon(uid, nukeOperativeComponent);

        args.StatusIcons.AddRange(healthIcons);
    }

    private IReadOnlyList<StatusIconPrototype> SyndicateIcon(EntityUid uid, NukeOperativeComponent nukeOperativeComponent)
    {
        var result = new List<StatusIconPrototype>();

        if (_prototype.TryIndex<StatusIconPrototype>(nukeOperativeComponent.SyndStatusIcon, out var syndicateicon))
        {
            result.Add(syndicateicon);
        }

        return result;
    }
}

