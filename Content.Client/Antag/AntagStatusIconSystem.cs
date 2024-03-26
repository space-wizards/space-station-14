using Content.Shared.Revolutionary.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Client.Antag;

/// <summary>
/// Used for assigning specified icons for antags.
/// </summary>
public sealed class AntagStatusIconSystem : SharedStatusIconSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>((uid, comp, ev) => GetRevIcon(uid, comp.StatusIcon, ref ev));
        SubscribeLocalEvent<ZombieComponent, GetStatusIconsEvent>((_, comp, ev) => GetIcon(comp.StatusIcon, ref ev));
        SubscribeLocalEvent<HeadRevolutionaryComponent, GetStatusIconsEvent>((_, comp, ev) => GetIcon(comp.StatusIcon, ref ev));
    }

    /// <summary>
    /// Adds a Status Icon on an entity if the player is supposed to see it.
    /// </summary>
    private void GetIcon(ProtoId<StatusIconPrototype> statusIcon, ref GetStatusIconsEvent ev)
    {
        ev.StatusIcons.Add(_prototype.Index(statusIcon));
    }

    /// <summary>
    /// Adds the Rev Icon on an entity if the player is supposed to see it. This additional function is needed to deal
    /// with a special case where if someone is a head rev we only want to display the headrev icon.
    /// </summary>
    private void GetRevIcon(EntityUid uid, ProtoId<StatusIconPrototype> statusIcon, ref GetStatusIconsEvent ev)
    {
        if (HasComp<HeadRevolutionaryComponent>(uid))
            return;

        GetIcon(statusIcon, ref ev);
    }
}
