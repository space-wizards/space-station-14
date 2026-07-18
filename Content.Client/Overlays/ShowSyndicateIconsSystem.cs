using Content.Shared.Overlays;
using Content.Shared.NukeOps;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed partial class ShowSyndicateIconsSystem : EquipmentHudSystem<ShowSyndicateIconsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeOperativeComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, NukeOperativeComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (ProtoMan.TryIndex<FactionIconPrototype>(component.SyndStatusIcon, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
