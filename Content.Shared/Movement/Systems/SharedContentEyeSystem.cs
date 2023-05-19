using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Movement.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Players;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Lets set zoom directly(with console and keys)
/// </summary>
public abstract class SharedContentEyeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    protected ISawmill Sawmill = Logger.GetSawmill("ceye");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
        SubscribeAllEvent<RequestFovEvent>(OnRequestFov);

        // DONT FORGET info
        Sawmill.Level = LogLevel.Debug;
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

        var current = Vector2.ComponentMin(component.MaxZoom, eyeComp.Zoom);
        SetDirtyTargetZoom(component, current);
    }

    public ContentEyeComponent? HasGhostZoom(ICommonSession? session, EntityUid? playerUid)
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

    public void SetDirtyTargetZoom(ContentEyeComponent content, Vector2 zoom)
    {
        content.TargetZoom = zoom;
        Dirty(content);
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
