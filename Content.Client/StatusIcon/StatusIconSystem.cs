using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Graphics;

namespace Content.Client.StatusIcon;

/// <summary>
/// This handles rendering gathering and rendering icons on entities.
/// </summary>
public sealed class StatusIconSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        if (!_overlay.HasOverlay<StatusIconOverlay>())
            _overlay.AddOverlay(new StatusIconOverlay());
    }

    public List<StatusIconData> GetStatusIcons(EntityUid uid)
    {
        if (!Exists(uid) || Terminating(uid))
            return new();

        var ev = new GetStatusIconsEvent(new());
        RaiseLocalEvent(uid, ref ev);
        return ev.StatusIcons;
    }
}

