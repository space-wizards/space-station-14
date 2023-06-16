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

    private const float ZoomMod = 1.5f;
    public readonly Vector2 DefaultZoom = Vector2.One;

    protected ISawmill Sawmill = Logger.GetSawmill("ceye");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
        SubscribeAllEvent<RequestTargetZoomEvent>(OnContentZoomRequest);
        SubscribeAllEvent<RequestFovEvent>(OnRequestFov);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ZoomIn, new ScrollInputCmdHandler(true, this))
            .Bind(ContentKeyFunctions.ZoomOut, new ScrollInputCmdHandler(false, this))
            .Bind(ContentKeyFunctions.ResetZoom, new ResetZoomInputCmdHandler(this))
            .Register<SharedContentEyeSystem>();

        Sawmill.Level = LogLevel.Info;
        UpdatesOutsidePrediction = true;
    }

    private Vector2 CheckZoomValueClamping(Vector2 checkedZoom, ContentEyeComponent component)
    {
        var minZoom = Vector2.One * (float)Math.Pow(ZoomMod, -3);
        return Vector2.Clamp(checkedZoom, minZoom, component.MaxZoom);
    }

    private void OnContentZoomRequest(RequestTargetZoomEvent msg, EntitySessionEventArgs args)
    {
        if (!TryComp<ContentEyeComponent>(args.SenderSession.AttachedEntity, out var content))
            return;

        content.TargetZoom = CheckZoomValueClamping(msg.TargetZoom, content);
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

    protected void UpdateEye(EntityUid uid, ContentEyeComponent content, SharedEyeComponent eye, float frameTime)
    {
        var diff = content.TargetZoom - eye.Zoom;

        if (diff.LengthSquared < 0.00001f)
        {
            eye.Zoom = content.TargetZoom;
            Dirty(eye);
            return;
        }

        var change = diff * 8f * frameTime;

        eye.Zoom += change;
        Dirty(eye);
    }

    private void ResetZoom(ContentEyeComponent component)
    {
        var zoom = CheckZoomValueClamping(DefaultZoom, component);

        if (component.TargetZoom.Equals(Vector2.One))
            return;

        component.TargetZoom = Vector2.One;
        Dirty(component);
    }

    public void SetMaxZoom(EntityUid uid, Vector2 value, ContentEyeComponent? component = null)
    {
        if (Resolve(uid, ref component))
            component.MaxZoom = value;
    }

    private void Zoom(bool zoomIn, ContentEyeComponent component)
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

        actual = CheckZoomValueClamping(actual, component);

        if (actual.Equals(component.TargetZoom))
            return;

        component.TargetZoom = actual;
        Dirty(component);
        Sawmill.Debug($"Set target zoom to {actual}");
    }

    public void SetTargetZoomDirectly(Vector2 newZoom, EntityUid playerUid)
    {
        if (TryComp<ContentEyeComponent>(playerUid, out var content)
            || content is not ContentEyeComponent comp)
            return;

        content.TargetZoom = newZoom;
        Dirty(content);
    }

    private ContentEyeComponent? GetContentFromCmdMessage(ICommonSession? session, InputCmdMessage message)
    {
        if (message is not FullInputCmdMessage full || full.State != BoundKeyState.Down)
            return null;

        if (session?.AttachedEntity == null
            || !TryComp<ContentEyeComponent>(session.AttachedEntity, out var component))
        {
            return null;
        }

        return component;
    }

    private sealed class ResetZoomInputCmdHandler : InputCmdHandler
    {
        private readonly SharedContentEyeSystem _system;

        public ResetZoomInputCmdHandler(SharedContentEyeSystem system)
        {
            _system = system;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (_system.GetContentFromCmdMessage(session, message) is ContentEyeComponent comp)
                _system.ResetZoom(comp);

            return false;
        }
    }

    private sealed class ScrollInputCmdHandler : InputCmdHandler
    {
        private readonly bool _zoomIn;
        private readonly SharedContentEyeSystem _system;

        public ScrollInputCmdHandler(bool zoomIn, SharedContentEyeSystem system)
        {
            _zoomIn = zoomIn;
            _system = system;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (_system.GetContentFromCmdMessage(session, message) is ContentEyeComponent comp)
                _system.Zoom(_zoomIn, comp);

            return false;
        }
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
}