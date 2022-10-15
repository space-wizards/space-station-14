using Content.Client.Administration.Managers;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private int _scrollAmount;

    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ZoomIn,  new ScrollInputCmdHandler(true, this))
            .Bind(ContentKeyFunctions.ZoomOut, new ScrollInputCmdHandler(false, this))
            .Bind(ContentKeyFunctions.ResetZoom, new ResetZoomInputCmdHandler(this))
            .Register<ContentEyeSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<ContentEyeSystem>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_scrollAmount != 0 && TryComp<ContentEyeComponent>(_playerManager.LocalPlayer?.ControlledEntity, out var eyeComp))
        {
            Vector2 target;

            if (_scrollAmount < 0f)
            {
                target = eyeComp.TargetZoom / ZoomChange * _scrollAmount * -1;
            }
            else
            {
                target = eyeComp.TargetZoom * ZoomChange * _scrollAmount;
            }

            target = Vector2.ComponentMax(MinZoom, target);
            target = Vector2.ComponentMin(MaxZoom, target);

            RaisePredictiveEvent(new ContentEyeZoomEvent()
            {
                Zoom = target,
            });
        }

        _scrollAmount = 0;
    }

    private void HandleInput(bool up)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _scrollAmount += (up ? 1 : -1);
    }

    private sealed class ResetZoomInputCmdHandler : InputCmdHandler
    {
        private readonly ContentEyeSystem _system;

        public ResetZoomInputCmdHandler(ContentEyeSystem system)
        {
            _system = system;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class ScrollInputCmdHandler : InputCmdHandler
    {
        private readonly bool _zoomIn;
        private readonly ContentEyeSystem _system;

        public ScrollInputCmdHandler(bool zoomIn, ContentEyeSystem system)
        {
            _zoomIn = zoomIn;
            _system = system;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full || session?.AttachedEntity == null ||
                full.State != BoundKeyState.Down)
            {
                return false;
            }

            _system.HandleInput(_zoomIn);
            return false;
        }
    }
}
