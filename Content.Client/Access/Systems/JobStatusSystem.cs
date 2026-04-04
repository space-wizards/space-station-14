using Content.Client.Overlays;
using Content.Shared.Access.Systems;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Access.Systems;

public sealed class JobStatusSystem : SharedJobStatusSystem
{
    [Dependency] private readonly ShowJobIconsSystem _showJobIcons = default!;
    [Dependency] private readonly ShowCrewIconsSystem _showCrewIcons = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<SecurityIconPrototype> CrewBorderIcon = "CrewBorderIcon";
    private static readonly ProtoId<SecurityIconPrototype> CrewUncertainBorderIcon = "CrewUncertainBorderIcon";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobStatusComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    // show the status icons if the player has the correponding HUDs
    private void OnGetStatusIconsEvent(Entity<JobStatusComponent> ent, ref GetStatusIconsEvent ev)
    {
        if (_showJobIcons.IsActive && ent.Comp.JobStatusIcon != null)
            ev.StatusIcons.Add(_prototype.Index(ent.Comp.JobStatusIcon));

        if (_showCrewIcons.IsActive)
        {
            if (_showCrewIcons.UncertainCrewBorder)
                ev.StatusIcons.Add(_prototype.Index(CrewUncertainBorderIcon));
            else if (ent.Comp.IsCrew)
                ev.StatusIcons.Add(_prototype.Index(CrewBorderIcon));
        }
    }
}
