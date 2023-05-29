using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public void RequestZoom(EntityUid uid, Vector2 zoom, ContentEyeComponent? content = null)
    {
        if (!Resolve(uid, ref content, false))
            return;

        RaisePredictiveEvent(new RequestTargetZoomEvent()
        {
            TargetZoom = zoom,
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var localPlayer = _player.LocalPlayer?.ControlledEntity;

        if (!TryComp<ContentEyeComponent>(localPlayer, out var content)
            || !TryComp<EyeComponent>(localPlayer, out var eyeComp))
        {
            return;
        }

        if (eyeComp.Zoom.Equals(content.TargetZoom))
            return;

        var diff = content.TargetZoom - eyeComp.Zoom;

        if (diff.LengthSquared < 0.000001f)
        {
            eyeComp.Zoom = content.TargetZoom;
            RaisePredictiveEvent(new EndOfTargetZoomAnimation());
            return;
        }

        var change = diff * 10 * frameTime;

        eyeComp.Zoom += change;
    }
}