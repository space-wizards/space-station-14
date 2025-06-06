using Content.Client.Overlays;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Access;

public sealed class JobStatusSystem : EntitySystem
{
    [Dependency] private readonly ShowJobIconsSystem _showJobIcons = default!;
    [Dependency] private readonly ShowCrewBorderIconsSystem _showCrewBorder = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<JobIconPrototype> JobIconForNoId = "JobIconNoId";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobStatusComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(Entity<JobStatusComponent> entity, ref GetStatusIconsEvent ev)
    {
        if (_showJobIcons.IsActive || _showCrewBorder.IsActive)
        {
            var iconId = GetEntityJobIcon(entity);

            if (_prototype.TryIndex<JobIconPrototype>(iconId, out var iconPrototype))
            {
                _showJobIcons.TryShowIcon(iconPrototype, ref ev);
                _showCrewBorder.TryShowIcon(iconPrototype, ref ev);

            }
            else
            {
                Log.Error($"Invalid job icon prototype: {iconPrototype}");
            }
        }
    }

    public ProtoId<JobIconPrototype> GetEntityJobIcon(EntityUid uid)
    {
        var iconId = JobIconForNoId;

        // TODO: Refactor this out into being a component property rather than a check;
        // If a HUD is active, this gets checked every draw call, which makes it decently expensive.
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

        return iconId;
    }
}
