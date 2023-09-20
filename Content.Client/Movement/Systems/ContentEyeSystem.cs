using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;

namespace Content.Client.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public void RequestZoom(EntityUid uid, Vector2 zoom, bool ignoreLimit, ContentEyeComponent? content = null)
    {
        if (!Resolve(uid, ref content, false))
            return;

        RaisePredictiveEvent(new RequestTargetZoomEvent()
        {
            TargetZoom = zoom,
            IgnoreLimit = ignoreLimit,
        });
    }

    public void RequestToggleFov()
    {
        if (_player.LocalPlayer?.ControlledEntity is { } player)
            RequestToggleFov(player);
    }

    public void RequestToggleFov(EntityUid uid, EyeComponent? eye = null)
    {
        if (Resolve(uid, ref eye, false))
            RequestFov(!eye.DrawFov);
    }

    public void RequestFov(bool value)
    {
        RaisePredictiveEvent(new RequestFovEvent()
        {
            Fov = value,
        });
    }
}
