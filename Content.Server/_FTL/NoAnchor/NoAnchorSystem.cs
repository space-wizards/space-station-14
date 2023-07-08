using Content.Server.Popups;
using Content.Shared.Construction.Components;

namespace Content.Server._FTL.NoAnchor;

/// <summary>
/// This handles preventing anchoring/unanchoring of a component
/// </summary>
public sealed class NoAnchorSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NoAnchorComponent, UnanchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<NoAnchorComponent, AnchorAttemptEvent>(OnUnanchorAttempt);
    }

    private void OnUnanchorAttempt(EntityUid uid, NoAnchorComponent component, AnchorAttemptEvent args)
    {
        if (!component.StopOnUnanchorAttempt)
            return;
        args.Cancel();
        _popupSystem.PopupEntity(Loc.GetString("unanchor-ftl-message"), uid);
    }

    private void OnAnchorAttempt(EntityUid uid, NoAnchorComponent component, UnanchorAttemptEvent args)
    {
        if (!component.StopOnAnchorAttempt)
            return;
        args.Cancel();
        _popupSystem.PopupEntity(Loc.GetString("unanchor-ftl-message"), uid);
    }
}
