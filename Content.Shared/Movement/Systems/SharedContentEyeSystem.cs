using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
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
    public Vector2 DefaultZoom { get; } = Vector2.One;
    private static readonly Vector2 MinZoom = Vector2.One * (float)Math.Pow(ZoomMod, -2);

    protected ISawmill Sawmill = Logger.GetSawmill("ceye");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
        SubscribeAllEvent<RequestTargetZoomEvent>(OnContentZoomRequest);
        SubscribeAllEvent<RequestFovEvent>(OnRequestFov);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ZoomIn, new KeyBindsInputCmdHandler(KeyBindsTypes.ZoomIn, this))
            .Bind(ContentKeyFunctions.ZoomOut, new KeyBindsInputCmdHandler(KeyBindsTypes.ZoomOut, this))
            .Bind(ContentKeyFunctions.ResetZoom, new KeyBindsInputCmdHandler(KeyBindsTypes.Reset, this))
            .Register<SharedContentEyeSystem>();

        Sawmill.Level = LogLevel.Info;
    }

    private Vector2 CheckZoomValue(Vector2 checkedZoom, ContentEyeComponent component)
    {
        var returnedZoom = Vector2.ComponentMax(MinZoom, checkedZoom);
        return Vector2.ComponentMin(component.MaxZoom, returnedZoom);
    }

    private void OnContentZoomRequest(RequestTargetZoomEvent msg, EntitySessionEventArgs args)
    {
        if (HasContentEyeComp(args.SenderSession) is not ContentEyeComponent content)
            return;

        content.TargetZoom = CheckZoomValue(msg.TargetZoom, content);
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

    private void ChangeZoomForType(KeyBindsTypes typeZoom, ICommonSession session)
    {
        if (HasContentEyeComp(session) is not ContentEyeComponent content)
            return;

        switch (typeZoom)
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

    private ContentEyeComponent? HasContentEyeComp(ICommonSession? session, EntityUid? playerUid = null)
    {
        var uid = session != null ? session.AttachedEntity : playerUid;

        if (uid is EntityUid entityUid
            && TryComp<ContentEyeComponent>(entityUid, out var ghostComp))
        {
            return ghostComp;
        }
        else
        {
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
        Sawmill.Debug($"Set reset zoom for user");
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

        actual = CheckZoomValue(actual, component);

        if (actual.Equals(component.TargetZoom))
            return;

        component.TargetZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set target zoom to {actual}");
    }

    public void SetTargetZoomDirectly(Vector2 newZoom, EntityUid playerUid)
    {
        if (HasContentEyeComp(null, playerUid) is not ContentEyeComponent content)
            return;

        content.TargetZoom = newZoom;
        Dirty(content);
    }

    private sealed class KeyBindsInputCmdHandler : InputCmdHandler
    {
        private readonly KeyBindsTypes _typeBind;
        private readonly SharedContentEyeSystem _system;

        public KeyBindsInputCmdHandler(KeyBindsTypes bind, SharedContentEyeSystem system)
        {
            _typeBind = bind;
            _system = system;
        }

        public override bool HandleCmdMessage(ICommonSession? commonSession, InputCmdMessage message)
        {
            if (commonSession is not ICommonSession session)
                return false;

            if (message is not FullInputCmdMessage full || full.State != BoundKeyState.Down)
                return false;

            _system.ChangeZoomForType(_typeBind, session);

            return false;
        }
    }

    private enum KeyBindsTypes : byte
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
    /// the end target animation on client
    /// set Zoom for server side
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class EndOfTargetZoomAnimation : EntityEventArgs
    {
    }
}