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

        // DONT FORGET info
        Sawmill.Level = LogLevel.Debug;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eyesSharedComponents = AllEntityQuery<SharedEyeComponent>();

        while (eyesSharedComponents.MoveNext(out var uid, out var eyeComp))
        {

            Vector2 targetZoom;
            if (HasGhostZoom(null, uid) is ContentEyeComponent ghostContent)
                targetZoom = ghostContent.TargetZoom;
            else
                targetZoom = eyeComp.AnimatedZoom;

            // set new value if they dont equals
            if (eyeComp.Zoom.Equals(targetZoom))
                continue;

            var diff = targetZoom - eyeComp.Zoom;

            if (diff.LengthSquared < 0.000001f)
            {
                eyeComp.Zoom = targetZoom;
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
        if (HasGhostZoom(args.SenderSession) is not ContentEyeComponent content)
            return;
        content.TargetZoom = msg.TargetZoom;
        Dirty(content);
    }

    private void OnChangeZoomRquest(RequestPlayeChangeZoomEvent msg, EntitySessionEventArgs args)
    {
        if (_lastTimeTagKeyZoomRequest >= msg.EventTimeTag)
            return;

        _lastTimeTagKeyZoomRequest = msg.EventTimeTag;

        if (HasGhostZoom(args.SenderSession) is ContentEyeComponent ghostComponent)
        {
            switch (msg.TypeZoom)
            {
                case KeyBindsTypes.ZoomIn:
                    GhostZoom(ghostComponent, true);
                    return;
                case KeyBindsTypes.ZoomOut:
                    GhostZoom(ghostComponent, false);
                    return;
                default:
                    ResetGhostZoom(ghostComponent);
                    return;
            }
        }
        else if (msg.PlayerUid is EntityUid player &&
            TryComp<SharedEyeComponent>(player, out var eyeComponent))
        {
            switch (msg.TypeZoom)
            {
                case KeyBindsTypes.ZoomIn:
                    UserZoom(eyeComponent, true);
                    return;
                case KeyBindsTypes.ZoomOut:
                    UserZoom(eyeComponent, false);
                    return;
                default:
                    ResetUserZoom(eyeComponent);
                    return;
            }
        }
    }

    private void ResetGhostZoom(ContentEyeComponent component)
    {
        if (component.TargetZoom.Equals(DefaultZoom))
            return;

        component.TargetZoom = DefaultZoom;
        Dirty(component);
    }

    private void ResetUserZoom(SharedEyeComponent component)
    {
        if (component.Zoom.Equals(DefaultZoom))
            return;

        component.AnimatedZoom = DefaultZoom;
        Dirty(component);
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

    private void GhostZoom(ContentEyeComponent component, bool zoomIn)
    {
        var actual = CalcZoom(
            zoomIn, component.TargetZoom, component.MaxZoom, MinZoom);

        if (actual.Equals(component.TargetZoom))
            return;

        component.TargetZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set target zoom to {actual}");
    }

    private void UserZoom(SharedEyeComponent component, bool zoomIn)
    {
        var actual = CalcZoom(
            zoomIn, component.Zoom, DefaultZoom, MinZoom);

        if (actual.Equals(component.AnimatedZoom))
            return;

        component.AnimatedZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set user zoom to {actual}");
    }
}
