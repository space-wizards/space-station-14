using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowSatiationIconsSystem : EquipmentHudSystem<ShowHungerIconsComponent>
{
    [Dependency] private readonly SatiationSystem _satiation = default!;

    private readonly ProtoId<SatiationTypePrototype> _satiationHunger = "hunger";
    private readonly ProtoId<SatiationTypePrototype> _satiationThirst = "thirst";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SatiationComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, SatiationComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive || ev.InContainer)
            return;

        if (_satiation.TryGetStatusIconPrototype(component, _satiationHunger, out var hungerIconPrototype))
            ev.StatusIcons.Add(hungerIconPrototype);
        if (_satiation.TryGetStatusIconPrototype(component, _satiationThirst, out var thirstIconPrototype))
            ev.StatusIcons.Add(thirstIconPrototype);
    }
}
