using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Movement.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Serialization;
using Robust.Shared.Players;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Lets specific sessions scroll and set their zoom directly.
/// </summary>
public abstract class SharedContentEyeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    private const float ZoomMod = 1.2f;
    private const byte ZoomMultiple = 10;

    private Vector2 MaxUserZoom { get; } = new Vector2(1.1f, 1.1f);
    private Vector2 DefaultZoom { get; } = Vector2.One;

    protected static readonly Vector2 MinZoom = new(
        MathF.Pow(ZoomMod, -ZoomMultiple),
        MathF.Pow(ZoomMod, -ZoomMultiple)
    );

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
        {
            return;
        }

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
        if (HasGhostZoom(args.SenderSession) is ContentEyeComponent ghostComponent)
        {
            switch (msg.TypeZoom)
            {
                case KeyBindsTypes.ZoomIn:
                    GhostZoom(ghostComponent, true);
                    break;
                case KeyBindsTypes.ZoomOut:
                    GhostZoom(ghostComponent, false);
                    break;
                default:
                    ResetGhostZoom(ghostComponent);
                    break;
            }
        }
        else if (msg.PlayerUid is EntityUid player &&
            TryComp<SharedEyeComponent>(player, out var eyeComponent))
        {
            switch (msg.TypeZoom)
            {
                case KeyBindsTypes.ZoomIn:
                    UserZoom(eyeComponent, true);
                    break;
                case KeyBindsTypes.ZoomOut:
                    UserZoom(eyeComponent, false);
                    break;
                default:
                    ResetUserZoom(eyeComponent);
                    break;
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

    protected void UpdateEye(EntityUid uid, ContentEyeComponent content, SharedEyeComponent eye, float frameTime)
    {
        var diff = content.TargetZoom - eye.Zoom;

        if (diff.LengthSquared < 0.0000001f)
        {
            eye.Zoom = content.TargetZoom;
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

        component.Zoom = DefaultZoom;
        Dirty(component);
    }

    private static Vector2 CalcZoom(bool zoomIn, Vector2 current, Vector2 maxZoom)
    {
        if (zoomIn)
        {
            current /= ZoomMod;
        }
        else
        {
            current *= ZoomMod;
        }

        current = Vector2.ComponentMax(MinZoom, current);
        current = Vector2.ComponentMin(maxZoom, current);

        return current;
    }

    private void GhostZoom(ContentEyeComponent component, bool zoomIn)
    {
        var actual = CalcZoom(zoomIn, component.TargetZoom, component.MaxZoom);

        if (actual.Equals(component.TargetZoom))
            return;

        component.TargetZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set target zoom to {actual}");
    }

    private void UserZoom(SharedEyeComponent component, bool zoomIn)
    {
        var actual = CalcZoom(zoomIn, component.Zoom, MaxUserZoom);

        if (actual.Equals(component.Zoom))
            return;

        component.Zoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set target for user zoom to {actual}");
    }

    private ContentEyeComponent? HasGhostZoom(ICommonSession? session)
    {
        if (session?.AttachedEntity is EntityUid entityUid
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
    /// user key bindings for request change zoom on server
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestPlayeChangeZoomEvent : EntityEventArgs
    {
        public EntityUid? PlayerUid;
        public KeyBindsTypes TypeZoom;
    }
}
