using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Movement.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Serialization;
using Robust.Shared.Players;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Lets set zoom directly(with console and keys)
/// </summary>
public abstract class SharedContentEyeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    private const float ZoomMod = 1.6f;
    private Vector2 DefaultZoom { get; } = Vector2.One;
    private static readonly Vector2 MinZoom = Vector2.One * MathF.Pow(ZoomMod, -3);

    private TimeSpan _lastTimeTagKeyZoomRequest;

    protected ISawmill Sawmill = Logger.GetSawmill("ceye");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
        SubscribeAllEvent<RequestTargetZoomEvent>(OnContentZoomRequest);
        SubscribeAllEvent<RequestFovEvent>(OnRequestFov);
        SubscribeAllEvent<RequestPlayeChangeZoomEvent>(OnChangeZoomRquest);

        // DONT FORGET info
        Sawmill.Level = LogLevel.Debug;
    }

    private void OnContentZoomRequest(RequestTargetZoomEvent msg, EntitySessionEventArgs args)
    {
        if (HasGhostZoom(args.SenderSession) is not ContentEyeComponent content)
            return;
        content.TargetZoom = msg.TargetZoom;
        Dirty(content);
    }

    private void OnRequestFov(RequestFovEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!HasComp<SharedGhostComponent>(player) && !_admin.IsAdmin(player))
            return;

        if (TryComp<SharedEyeComponent>(player, out var eyeComp))
        {
            eyeComp.DrawFov = msg.Fov;
            Dirty(eyeComp);
        }
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

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedContentEyeSystem>();
    }

    private void OnContentEyeStartup(EntityUid uid, ContentEyeComponent component, ComponentStartup args)
    {
        if (!TryComp<SharedEyeComponent>(uid, out var eyeComp))
            return;

        component.TargetZoom = eyeComp.Zoom;
        Dirty(component);
    }

    protected void UpdateEye(SharedEyeComponent eye, Vector2 targetZoom, float frameTime)
    {
        var diff = targetZoom - eye.Zoom;

        if (diff.LengthSquared < 0.0000001f)
        {
            eye.Zoom = targetZoom;
            Dirty(eye);
            return;
        }

        var change = diff * 8f * frameTime;

        eye.Zoom += change;
        Dirty(eye);
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

        component.TargetZoom = DefaultZoom;
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
        Logger.Debug($"current is {component.TargetZoom} +++++++++++");
        var actual = CalcZoom(
            zoomIn, component.TargetZoom, component.MaxZoom, MinZoom);

        if (actual.Equals(component.TargetZoom))
            return;

        Logger.Debug($"actual is {actual}");
        component.TargetZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set target zoom to {actual}");
    }

    private void UserZoom(SharedEyeComponent component, bool zoomIn)
    {
        var actual = CalcZoom(
            zoomIn, component.TargetZoom, component.MaxZoom, MinZoom);

        if (actual.Equals(component.TargetZoom))
            return;

        component.TargetZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set user zoom to {actual}");
    }

    public ContentEyeComponent? HasGhostZoom(ICommonSession? session, EntityUid? someUid = null)
    {
        var uid = session?.AttachedEntity ?? someUid;

        if (uid is EntityUid entityUid
            && TryComp<ContentEyeComponent>(entityUid, out var ghostComp))
            return ghostComp;
        else
            return null;
    }

    public enum KeyBindsTypes : byte
    {
        ZoomIn,
        ZoomOut,
        Reset,
    }

    /// <summary>
    /// Sendable from client to server to request a target zoom.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestTargetZoomEvent : EntityEventArgs
    {
        public Vector2 TargetZoom;
    }

    /// <summary>
    /// Sendable from client to server to request changing fov.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestFovEvent : EntityEventArgs
    {
        public bool Fov;
    }

    /// <summary>
    /// uses for send user zoom keys events
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestPlayeChangeZoomEvent : EntityEventArgs
    {
        public EntityUid? PlayerUid;
        public KeyBindsTypes TypeZoom;
        public TimeSpan EventTimeTag;
    }
}
