using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Movement.Components;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Lets specific sessions scroll and set their zoom directly.
/// </summary>
public abstract class SharedContentEyeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    private const float ZoomMod = 1.6f;
    private Vector2 DefaultZoom { get; } = Vector2.One;
    private static readonly Vector2 MinZoom = Vector2.One * MathF.Pow(ZoomMod, -3);

    protected ISawmill Sawmill = Logger.GetSawmill("ceye");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
        SubscribeAllEvent<RequestTargetZoomEvent>(OnContentZoomRequest);
        SubscribeAllEvent<RequestFovEvent>(OnRequestFov);
        SubscribeAllEvent<RequestPlayerChangeZoomEvent>(OnChangeZoomForType);

        Sawmill.Level = LogLevel.Debug;
    }

    private void OnContentZoomRequest(RequestTargetZoomEvent msg, EntitySessionEventArgs args)
    {
        if (HasContentEyeComp(args.SenderSession, null) is not ContentEyeComponent content)
            return;

        content.TargetZoom = Vector2.ComponentMin(msg.TargetZoom, content.MaxZoom);
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

    private void OnContentEyeStartup(EntityUid uid, ContentEyeComponent component, ComponentStartup args)
    {
        if (!TryComp<SharedEyeComponent>(uid, out var eyeComp))
            return;

        component.TargetZoom = Vector2.ComponentMin(component.MaxZoom, eyeComp.Zoom);
        Dirty(component);
    }

    public void OnChangeZoomForType(RequestPlayerChangeZoomEvent msg, EntitySessionEventArgs args)
    {
        Logger.Debug("zoom change!");
        if (HasContentEyeComp(args.SenderSession, msg.PlayerUid) is ContentEyeComponent content)
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

    public ContentEyeComponent? HasContentEyeComp(ICommonSession? session, EntityUid? playerUid)
    {
        var uid = session?.AttachedEntity ?? playerUid;

        if (uid is EntityUid entityUid
            && TryComp<ContentEyeComponent>(entityUid, out var ghostComp))
        {
            return ghostComp;
        }
        else
        {
            Sawmill.Debug($"don't find ContentEyeComponent for {uid}!");
            return null;
        }
    }

    private void ResetZoom(ContentEyeComponent component)
    {
        var actual = Vector2.ComponentMin(component.MaxZoom, DefaultZoom);

        if (component.TargetZoom.Equals(actual))
            return;

        component.TargetZoom = actual;
        Dirty(component);
    }

    private void Zoom(ContentEyeComponent component, bool zoomIn)
    {
        var actual = component.TargetZoom;

        if (zoomIn)
        {
            actual /= ZoomMod;
        }
        else
        {
            actual *= ZoomMod;
        }

        actual = Vector2.ComponentMax(MinZoom, actual);
        actual = Vector2.ComponentMin(component.MaxZoom, actual);

        if (actual.Equals(component.TargetZoom))
            return;

        component.TargetZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set target zoom to {actual}");
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
    public sealed class RequestPlayerChangeZoomEvent : EntityEventArgs
    {
        public EntityUid? PlayerUid;
        public KeyBindsTypes TypeZoom;
    }

    /// <summary>
    /// the end target animation on client
    /// set Zoom for server side
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class EndOfTargetZoomAnimation : EntityEventArgs { }
}