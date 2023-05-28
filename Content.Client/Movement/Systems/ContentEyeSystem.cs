using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Client.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ZoomIn, new KeyBindsInputCmdHandler(KeyBindsTypes.ZoomIn, this))
            .Bind(ContentKeyFunctions.ZoomOut, new KeyBindsInputCmdHandler(KeyBindsTypes.ZoomOut, this))
            .Bind(ContentKeyFunctions.ResetZoom, new KeyBindsInputCmdHandler(KeyBindsTypes.Reset, this))
            .Register<ContentEyeSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedContentEyeSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var localPlayer = _player.LocalPlayer?.ControlledEntity;

        if (HasContentEyeComp(null, localPlayer) is not ContentEyeComponent content
            || !TryComp<EyeComponent>(localPlayer, out var eyeComp))
        {
            return;
        }

        if (eyeComp.Zoom.Equals(content.TargetZoom))
            return;

        var diff = content.TargetZoom - eyeComp.Zoom;

        if (diff.LengthSquared < 0.00001f)
        {
            eyeComp.Zoom = content.TargetZoom;
            Logger.Debug("set target zoom ++++++++++++++++++++");
            RaisePredictiveEvent(new EndOfTargetZoomAnimation());
            return;
        }

        var change = diff * 10 * frameTime;

        eyeComp.Zoom += change;
    }

    public void RequestZoom(Vector2 zoom)
    {
        RaisePredictiveEvent(new RequestTargetZoomEvent()
        {
            TargetZoom = zoom
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

    private void OnZoomChangeKeyBind(KeyBindsTypes type)
    {
        RaisePredictiveEvent(new RequestPlayerChangeZoomEvent()
        {
            TypeZoom = type,
            PlayerUid = _player.LocalPlayer?.ControlledEntity,
        });
    }

    private sealed class KeyBindsInputCmdHandler : InputCmdHandler
    {
        private readonly KeyBindsTypes _typeBind;
        private readonly ContentEyeSystem _system;

        public KeyBindsInputCmdHandler(KeyBindsTypes bind, ContentEyeSystem system)
        {
            _typeBind = bind;
            _system = system;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full || full.State != BoundKeyState.Down)
                return false;

            _system.OnZoomChangeKeyBind(_typeBind);

            return true;
        }
    }
}