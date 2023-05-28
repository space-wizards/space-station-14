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

    private const float ZoomMod = 1.2f;
    private const byte ZoomMultiple = 10;

    protected static readonly Vector2 MinZoom = new(MathF.Pow(ZoomMod, -ZoomMultiple), MathF.Pow(ZoomMod, -ZoomMultiple));

    protected ISawmill Sawmill = Logger.GetSawmill("ceye");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
        SubscribeAllEvent<RequestTargetZoomEvent>(OnContentZoomRequest);
        SubscribeAllEvent<RequestFovEvent>(OnRequestFov);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ZoomIn,  new ScrollInputCmdHandler(true, this))
            .Bind(ContentKeyFunctions.ZoomOut, new ScrollInputCmdHandler(false, this))
            .Bind(ContentKeyFunctions.ResetZoom, new ResetZoomInputCmdHandler(this))
            .Register<SharedContentEyeSystem>();

        Sawmill.Level = LogLevel.Info;
    }

    private void OnContentZoomRequest(RequestTargetZoomEvent msg, EntitySessionEventArgs args)
    {
        if (!TryComp<ContentEyeComponent>(args.SenderSession.AttachedEntity, out var content))
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

    private bool CanZoom(EntityUid uid, ContentEyeComponent? component = null)
    {
        return Resolve(uid, ref component, false);
    }

    private void ResetZoom(EntityUid uid, ContentEyeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetZoom.Equals(Vector2.One))
            return;

        component.TargetZoom = Vector2.One;
        Dirty(component);
    }

    private void Zoom(EntityUid uid, bool zoomIn, ContentEyeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

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

    private sealed class ResetZoomInputCmdHandler : InputCmdHandler
    {
        private readonly SharedContentEyeSystem _system;

        public ResetZoomInputCmdHandler(SharedContentEyeSystem system)
        {
            _system = system;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            ContentEyeComponent? component = null;

            if (message is not FullInputCmdMessage full || session?.AttachedEntity == null ||
                full.State != BoundKeyState.Down ||
                !_system.CanZoom(session.AttachedEntity.Value, component))
            {
                return false;
            }

            _system.ResetZoom(session.AttachedEntity.Value, component);
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
            ContentEyeComponent? component = null;

            if (message is not FullInputCmdMessage full || session?.AttachedEntity == null ||
                full.State != BoundKeyState.Down ||
                !_system.CanZoom(session.AttachedEntity.Value, component))
            {
                return false;
            }

            _system.Zoom(session.AttachedEntity.Value, _zoomIn, component);
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
