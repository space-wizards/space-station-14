using Content.Shared.Antag;
using Content.Shared.Revolutionary.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Zombies;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Antag;

/// <summary>
/// Used for assigning specified icons for antags.
/// </summary>
public sealed class AntagStatusIconSystem : SharedStatusIconSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>(GetRevIcon);
        SubscribeLocalEvent<ZombieComponent, GetStatusIconsEvent>(GetIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, GetStatusIconsEvent>(GetIcon);
        SubscribeLocalEvent<InitialInfectedComponent, GetStatusIconsEvent>(GetIcon);
    }

    /// <summary>
    /// Adds a Status Icon on an entity if the player is supposed to see it.
    /// </summary>
    private void GetIcon<T>(EntityUid uid, T comp, ref GetStatusIconsEvent ev) where T: IAntagStatusIconComponent
    {
        var ent = _player.LocalSession?.AttachedEntity;

        var canEv = new CanDisplayStatusIconsEvent(ent);
        RaiseLocalEvent(uid, ref canEv);

        if (!canEv.Cancelled)
            ev.StatusIcons.Add(_prototype.Index(comp.StatusIcon));
    }


    /// <summary>
    /// Adds the Rev Icon on an entity if the player is supposed to see it. This additional function is needed to deal
    /// with a special case where if someone is a head rev we only want to display the headrev icon.
    /// </summary>
    private void GetRevIcon(EntityUid uid, RevolutionaryComponent comp, ref GetStatusIconsEvent ev)
    {
        if (HasComp<HeadRevolutionaryComponent>(uid))
            return;

        GetIcon(uid, comp, ref ev);

    }
}
