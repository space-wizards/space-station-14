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

    private void OnGetStatusIconsEvent(Entity<SatiationComponent> ent, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (!TryComp<ShowSatiationIconsComponent>(ent.Owner, out var showIcons))
            return;

        foreach (var satiation in showIcons.Satiations)
        {
            if (_satiation.TryGetStatusIconPrototype((ent.Owner, ent.Comp), satiation, out var iconPrototype))
                ev.StatusIcons.Add(iconPrototype);
        }
    }
}
