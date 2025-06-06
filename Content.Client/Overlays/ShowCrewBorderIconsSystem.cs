using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowCrewBorderIconsSystem : EquipmentHudSystem<ShowCrewBorderIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public bool IncludeCrewBorder = false;
    public bool UncertainCrewBorder = false;

    private static readonly ProtoId<SecurityIconPrototype> CrewBorder = "CrewBorderIcon";
    private static readonly ProtoId<SecurityIconPrototype> CrewUncertainBorder = "CrewUncertainBorderIcon";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowCrewBorderIconsComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowCrewBorderIconsComponent> component)
    {
        base.UpdateInternal(component);

        IncludeCrewBorder = false;
        UncertainCrewBorder = false;
        foreach (var comp in component.Components)
        {
            if (comp.IncludeCrewBorder)
                IncludeCrewBorder = true;

            if (comp.UncertainCrewBorder)
                UncertainCrewBorder = true;
        }
    }

    private void OnHandleState(Entity<ShowCrewBorderIconsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    public void TryShowIcon(JobIconPrototype iconPrototype, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (IncludeCrewBorder)
        {
            if (!UncertainCrewBorder)
            {
                if (iconPrototype.IsCrewJob && _prototype.TryIndex<SecurityIconPrototype>(CrewBorder, out var crewBorderIconPrototype))
                    ev.StatusIcons.Add(crewBorderIconPrototype);
            }
            else
            {
                if (_prototype.TryIndex<SecurityIconPrototype>(CrewUncertainBorder, out var crewBorderIconPrototype))
                    ev.StatusIcons.Add(crewBorderIconPrototype);
            }
        }
    }
}
