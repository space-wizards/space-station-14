using Content.Shared.Ghost;
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
        SubscribeLocalEvent<HeadRevolutionaryComponent, GetStatusIconsEvent>(GetHeadRevIcon);
        SubscribeLocalEvent<ZombieComponent, GetStatusIconsEvent>(GetZombieIcon);
    }

    /// <summary>
    /// Adds the Rev Icon on an entity if the player is supposed to see it.
    /// </summary>
    private void GetRevIcon(EntityUid uid, RevolutionaryComponent comp, ref GetStatusIconsEvent ev)
    {
        // This is necessary to make sure the rev icon does not get added when this is actually a headrev.
        // We cannot do this through the CanDisplayStatusIconsEvent because the rev system receives the event twice in
        // the case of headrevs and if it cancels it there then no icons will be added.
        if (HasComp<HeadRevolutionaryComponent>(uid))
            return;

        var ent = _player.LocalSession?.AttachedEntity;

        var canEv = new CanDisplayStatusIconsEvent(ent);
        RaiseLocalEvent(uid, ref canEv);

        if (!canEv.Cancelled)
            GetStatusIcon(comp.RevStatusIcon, ref ev);
    }

    /// <summary>
    /// Adds the Head Rev Icon on an entity if the player is supposed to see it.
    /// </summary>
    private void GetHeadRevIcon(EntityUid uid, HeadRevolutionaryComponent comp, ref GetStatusIconsEvent ev)
    {

        var ent = _player.LocalSession?.AttachedEntity;

        var canEv = new CanDisplayStatusIconsEvent(ent);
        RaiseLocalEvent(uid, ref canEv);

        if (!canEv.Cancelled)
            GetStatusIcon(comp.HeadRevStatusIcon, ref ev);
    }

    private void GetZombieIcon(EntityUid uid, ZombieComponent comp, ref GetStatusIconsEvent ev)
    {
        var ent = _player.LocalSession?.AttachedEntity;

        var canEv = new CanDisplayStatusIconsEvent(ent);
        RaiseLocalEvent(uid, ref canEv);

        if (!canEv.Cancelled)
            GetStatusIcon(comp.ZombieStatusIcon, ref ev);
    }

    /// <summary>
    /// Will check if the local player has the same component as the one who called it and give the status icon.
    /// </summary>
    /// <param name="antagStatusIcon">The status icon that your antag uses</param>
    /// <param name="args">The GetStatusIcon event.</param>
    private void GetStatusIcon(string antagStatusIcon, ref GetStatusIconsEvent args)
    {
        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(antagStatusIcon));
    }
}
