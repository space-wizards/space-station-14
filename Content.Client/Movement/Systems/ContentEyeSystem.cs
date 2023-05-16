using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private TimeSpan? _userZoomChangeRequestTimeOut = null;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ZoomIn, new KeyBindsInputCmdHandler(KeyBindsTypes.ZoomIn, this))
            .Bind(ContentKeyFunctions.ZoomOut, new KeyBindsInputCmdHandler(KeyBindsTypes.ZoomOut, this))
            .Bind(ContentKeyFunctions.ResetZoom, new KeyBindsInputCmdHandler(KeyBindsTypes.Reset, this))
            .Register<ContentEyeSystem>();
    }

    public void RequestZoom(EntityUid uid, Vector2 zoom, ContentEyeComponent? content = null)
    {
        if (!Resolve(uid, ref content, false))
            return;

        RaisePredictiveEvent(new RequestTargetZoomEvent()
        {
            TargetZoom = zoom,
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var localPlayer = _player.LocalPlayer?.ControlledEntity;

        if (!TryComp<ContentEyeComponent>(localPlayer, out var content) ||
            !TryComp<EyeComponent>(localPlayer, out var eye))
        {
            return;
        }

        UpdateEye(localPlayer.Value, content, eye, frameTime);
    }

    private void OnKeyBindZoomChange(KeyBindsTypes type)
    {
        if (_userZoomChangeRequestTimeOut != null
            && _userZoomChangeRequestTimeOut + TimeSpan.FromMicroseconds(500) > _gameTiming.CurTime)
        {
            return;
        }

        RaisePredictiveEvent(new RequestPlayeChangeZoomEvent()
        {
            TypeZoom = type,
            PlayerUid = _player.LocalPlayer?.ControlledEntity
        });

        _userZoomChangeRequestTimeOut = _gameTiming.CurTime;
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

            _system.OnKeyBindZoomChange(_typeBind);

            return false;
        }
    }
}
