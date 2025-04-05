using System.Linq;
using Content.Shared.Inventory.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
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

    private HashSet<ProtoId<SatiationTypePrototype>> _types = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SatiationComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowSatiationIconsComponent> args)
    {
        base.UpdateInternal(args);

        // Any time we update `ShowIcons` component, we need to reconstruct the set of satiation types to show.
        _types = [];
        foreach (var type in args.Components.SelectMany(comp => comp.ShownTypes))
        {
            _types.Add(type);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _types.Clear();
    }

    private void OnGetStatusIconsEvent(Entity<SatiationComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
        {
            return;
        }

        foreach (var shownTypeId in _types)
        {
            if (_satiation.GetStatusIconOrNull(entity, shownTypeId) is { } iconId)
            {
                args.StatusIcons.Add(iconId);
            }
        }
    }
}
