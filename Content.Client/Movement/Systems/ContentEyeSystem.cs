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

    private ushort _lastSubtickScroll;
    private float _scrollAmount;

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

        if (_scrollAmount != 0f && TryComp<ContentEyeComponent>(_playerManager.LocalPlayer?.ControlledEntity, out var eyeComp))
        {
            RaisePredictiveEvent(new ContentEyeZoomEvent()
            {
                Zoom = eyeComp.TargetZoom + eyeComp.TargetZoom / 2f * _scrollAmount,
            });
        }

        _scrollAmount = 0f;
        _lastSubtickScroll = 0;
    }

    private void HandleInput(bool up, ushort subTick)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var diff = subTick - _lastSubtickScroll;
        _lastSubtickScroll = subTick;
        var fraction = diff / (float) ushort.MaxValue;

        _scrollAmount += fraction * (up ? 1f : -1f);
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
            if (message is not FullInputCmdMessage full || session?.AttachedEntity == null)
                return false;

            _system.HandleInput(_zoomIn, full.SubTick);
            return false;
        }
    }
}
