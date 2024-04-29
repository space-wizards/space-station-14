using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed class ShowSatiationIconsSystem : EquipmentHudSystem<ShowHungerIconsComponent>
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

        if (_satiation.TryGetStatusHungerIconPrototype(component, out var hungerIconPrototype))
            ev.StatusIcons.Add(hungerIconPrototype);
        if (_satiation.TryGetStatusThirstIconPrototype(component, out var thirstIconPrototype))
            ev.StatusIcons.Add(thirstIconPrototype);
    }
}
