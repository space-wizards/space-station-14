using Content.Shared.Revolutionary.Components;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Configuration;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _revIconGhostVisibility;

    private void OnRevGhostIconVisibilityChanged(bool value) => _revIconGhostVisibility = value;
    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.RevIconsVisibleToGhosts, OnRevGhostIconVisibilityChanged, true);
        SubscribeLocalEvent<RevolutionaryComponent, CanDisplayStatusIconsEvent>(OnCanShowRevIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, CanDisplayStatusIconsEvent>(OnCanShowRevIcon);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.RevIconsVisibleToGhosts, OnRevGhostIconVisibilityChanged);
    }

    /// <summary>
    /// Determine whether a client should display the rev icon.
    /// </summary>
    private void OnCanShowRevIcon<T>(EntityUid uid, T comp, ref CanDisplayStatusIconsEvent args)
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
