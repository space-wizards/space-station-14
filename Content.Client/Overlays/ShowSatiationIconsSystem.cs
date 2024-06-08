using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed class ShowSatiationIconsSystem : EquipmentHudSystem<ShowSatiationIconsComponent>
{
    [Dependency] private readonly SatiationSystem _satiation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SatiationComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, SatiationComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive || ev.InContainer)
            return;

        if (!TryComp<ShowSatiationIconsComponent>(uid, out var showIcons))
            return;

        foreach (var satiation in showIcons.Satiations)
        {
            if (_satiation.TryGetStatusIconPrototype(component, satiation, out var iconPrototype))
                ev.StatusIcons.Add(iconPrototype);
        }
    }
}
