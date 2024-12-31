using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// This system handles the inclusion of satiation status icons for entities with the <see cref="ShowSatiationIconsComponent"/>
/// </summary>
public sealed class ShowSatiationIconsSystem : EquipmentHudSystem<ShowSatiationIconsComponent>
{
    [Dependency] private readonly SatiationSystem _satiation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SatiationComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(Entity<SatiationComponent> entity, ref GetStatusIconsEvent args)
    {
        // TODO I am not convinced that this `TryComp` is correct. It seems like it'll be looking for the component on
        //  the entity whose status is being checked, rather than an entity who is viewing. Eg. you put on the beer
        //  goggles or something and this system will be checking the PATRONS for the `can see thirst` component.
        if (!IsActive || !TryComp<ShowSatiationIconsComponent>(entity, out var showComp))
        {
            return;
        }

        foreach (var shownTypeId in showComp.ShownTypes)
        {
            if (_satiation.GetStatusIconOrNull(entity, shownTypeId) is { } iconId)
            {
                args.StatusIcons.Add(iconId);
            }
        }
    }
}
