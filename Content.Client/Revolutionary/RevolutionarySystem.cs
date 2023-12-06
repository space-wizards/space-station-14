using Content.Shared.Revolutionary.Components;
using Content.Client.Antag;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Configuration;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : SharedStatusIconSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _revIconGhostVisibility;
    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.RevIconsVisibleToGhosts, value => _revIconGhostVisibility = value, true);
        SubscribeLocalEvent<RevolutionaryComponent, CanDisplayStatusIconsEvent>(OnCanShowRevIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, CanDisplayStatusIconsEvent>(OnCanShowHeadRevIcon);
    }

    /// <summary>
    /// Determine whether a client should display the rev icon.
    /// </summary>
    private void OnCanShowRevIcon(EntityUid uid, RevolutionaryComponent comp, ref CanDisplayStatusIconsEvent args)
    {
        args.Cancelled = !CanDisplayIcon(args.User);
    }

    /// <summary>
    /// Determine whether a client should display the head rev icon.
    /// </summary>
    private void OnCanShowHeadRevIcon(EntityUid uid, HeadRevolutionaryComponent comp, ref CanDisplayStatusIconsEvent args)
    {
        args.Cancelled = !CanDisplayIcon(args.User);
    }

    /// <summary>
    /// The criteria that determine whether a client should see Rev/Head rev icons.
    /// </summary>
    private bool CanDisplayIcon(EntityUid? uid)
    {
        if (HasComp<HeadRevolutionaryComponent>(uid) || HasComp<RevolutionaryComponent>(uid))
            return true;

        if (_revIconGhostVisibility && HasComp<GhostComponent>(uid))
            return true;

        return HasComp<ShowRevIconsComponent>(uid);


    }

}
