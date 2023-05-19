using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    private const float ZoomMod = 1.6f;
    private Vector2 DefaultZoom { get; } = Vector2.One;
    private static readonly Vector2 MinZoom = Vector2.One * MathF.Pow(ZoomMod, -3);

    private TimeSpan _lastTimeTagKeyZoomRequest;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestTargetZoomEvent>(OnContentZoomRequest);
        SubscribeAllEvent<RequestPlayeChangeZoomEvent>(OnChangeZoomRquest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<ContentEyeComponent, SharedEyeComponent>();

        while (query.MoveNext(out var uid, out var comp, out var eyeComp))
        {
            if (!comp.IsProcessed || eyeComp.Zoom.Equals(comp.TargetZoom))
                continue;

            var diff = comp.TargetZoom - eyeComp.Zoom;

            if (diff.LengthSquared < 0.000001f)
            {
                comp.IsProcessed = false;
                eyeComp.Zoom = comp.TargetZoom;
                Dirty(eyeComp);
                return;
            }

            var change = diff * 5f * frameTime;

            eyeComp.Zoom += change;
            Dirty(eyeComp);
        }
    }

    private void OnContentZoomRequest(RequestTargetZoomEvent msg, EntitySessionEventArgs args)
    {
        if (HasGhostZoom(args.SenderSession, msg.PlayerUid) is ContentEyeComponent content)
            SetDirtyTargetZoom(content, msg.TargetZoom);
    }

    private void OnChangeZoomRquest(RequestPlayeChangeZoomEvent msg, EntitySessionEventArgs args)
    {
        if (_lastTimeTagKeyZoomRequest >= msg.EventTimeTag)
            return;

        _lastTimeTagKeyZoomRequest = msg.EventTimeTag;

        if (HasGhostZoom(args.SenderSession, msg.PlayerUid) is ContentEyeComponent content)
        {
            switch (msg.TypeZoom)
            {
                case KeyBindsTypes.ZoomIn:
                    Zoom(content, true);
                    return;
                case KeyBindsTypes.ZoomOut:
                    Zoom(content, false);
                    return;
                default:
                    ResetZoom(content);
                    return;
            }
        }
    }

    private void ResetZoom(ContentEyeComponent component)
    {
        if (component.TargetZoom.Equals(DefaultZoom))
            return;

        SetDirtyTargetZoom(component, DefaultZoom);
    }

    private void Zoom(ContentEyeComponent component, bool zoomIn)
    {
        var actual = CalcZoom(
            zoomIn, component.TargetZoom, component.MaxZoom, MinZoom);

        if (actual.Equals(component.TargetZoom))
            return;

        SetDirtyTargetZoom(component, actual);
        Sawmill.Debug($"Set target zoom to {actual}");
    }

    private static Vector2 CalcZoom(bool zoomIn, Vector2 current, Vector2 maxZoom, Vector2 minZoom)
    {
        if (zoomIn)
        {
            current /= ZoomMod;
        }
        else
        {
            current *= ZoomMod;
        }

        current = Vector2.ComponentMax(minZoom, current);
        current = Vector2.ComponentMin(maxZoom, current);

        return current;
    }
}
