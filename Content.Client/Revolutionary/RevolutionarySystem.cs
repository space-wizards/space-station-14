using Content.Shared.Antag;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, CanDisplayStatusIconsEvent>(OnCanShowRevIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, CanDisplayStatusIconsEvent>(OnCanShowRevIcon);
    }

    /// <summary>
    /// Determine whether a client should display the rev icon.
    /// </summary>
    private void OnCanShowRevIcon<T>(EntityUid uid, T comp, ref CanDisplayStatusIconsEvent args) where T : IAntagStatusIconComponent
    {
        args.Cancelled = !CanDisplayIcon(args.User, comp.IconVisibleToGhost);
    }

    /// <summary>
    /// The criteria that determine whether a client should see Rev/Head rev icons.
    /// </summary>
    private bool CanDisplayIcon(EntityUid? uid, bool visibleToGhost)
    {
        if (HasComp<HeadRevolutionaryComponent>(uid) || HasComp<RevolutionaryComponent>(uid))
            return true;

        if (visibleToGhost && HasComp<GhostComponent>(uid))
            return true;

        return HasComp<ShowRevIconsComponent>(uid);
    }

}
