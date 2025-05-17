using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowJobIconsSystem : EquipmentHudSystem<ShowJobIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    [ValidatePrototypeId<JobIconPrototype>]
    private const string JobIconForNoId = "JobIconNoId";

    [ValidatePrototypeId<SecurityIconPrototype>]
    private const string CrewBorder = "CrewBorderIcon";
    [ValidatePrototypeId<SecurityIconPrototype>]
    private const string CrewUncertainBorder = "CrewUncertainBorderIcon";

    [ViewVariables]
    public bool IncludeCrewBorder = false;

    [ViewVariables]
    public bool UncertainCrewBorder = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<ShowJobIconsComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<ShowJobIconsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowJobIconsComponent> component)
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

    private void OnGetStatusIconsEvent(EntityUid uid, StatusIconComponent _, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        var iconId = JobIconForNoId;

        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp<IdCardComponent>(item, out var id))
                {
                    iconId = id.JobIcon;
                    break;
                }

                // PDA
                if (TryComp<PdaComponent>(item, out var pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    iconId = id.JobIcon;
                    break;
                }
            }
        }

        if (_prototype.TryIndex<JobIconPrototype>(iconId, out var iconPrototype))
        {
            ev.StatusIcons.Add(iconPrototype);
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
        else
        {
            Log.Error($"Invalid job icon prototype: {iconPrototype}");
        }
    }
}
