using System;
using System.Threading;
using Content.Shared.GameObjects.Components.Weapons;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components.Weapons
{
    [RegisterComponent]
    public sealed class ClientFlashableComponent : SharedFlashableComponent
    {
        private CancellationTokenSource _cancelToken;
        private double _duration;
        private FlashOverlay _overlay;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
            {
                return;
            }

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            if (playerManager.LocalPlayer.ControlledEntity != Owner)
            {
                return;
            }

            var newState = (FlashComponentState) curState;
            if (newState.Time == default)
            {
                return;
            }

            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime;
            // Account for ping
            _duration = newState.Duration - (currentTime - newState.Time).TotalSeconds;
            EnableOverlay();
        }

        private void EnableOverlay()
        {
            // If the timer gets reset
            if (_overlay != null)
            {
                _overlay.Reset();
                _cancelToken.Cancel();
            }
            else
            {
                var overlayManager = IoCManager.Resolve<IOverlayManager>();
                _overlay = new FlashOverlay(_duration);
                overlayManager.AddOverlay(_overlay);
            }

            _cancelToken = new CancellationTokenSource();
            Timer.Spawn((int) _duration * 1000, DisableOverlay, _cancelToken.Token);
        }

        private void DisableOverlay()
        {
            if (_overlay == null)
            {
                return;
            }

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            overlayManager.RemoveOverlay(_overlay.ID);
            _overlay = null;
            _cancelToken.Cancel();
            _cancelToken = null;
        }
    }

    public sealed class FlashOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private IGameTiming _timer;
        private IClyde _displayManager;
        private TimeSpan _startTime;
        private readonly double _duration;
        public FlashOverlay(double duration) : base(nameof(FlashOverlay))
        {
            _timer = IoCManager.Resolve<IGameTiming>();
            _displayManager = IoCManager.Resolve<IClyde>();
            _startTime = _timer.CurTime;
            _duration = duration;
        }

        public void Reset()
        {
            _startTime = _timer.CurTime;
        }

        protected override void Draw(DrawingHandleBase handle)
        {
            var elapsedTime = (_timer.CurTime - _startTime).TotalSeconds;
            if (elapsedTime > _duration)
            {
                return;
            }
            var screenHandle = (DrawingHandleScreen) handle;

            screenHandle.DrawRect(
                new UIBox2(0.0f, 0.0f, _displayManager.ScreenSize.X, _displayManager.ScreenSize.Y),
                Color.White.WithAlpha(GetAlpha(elapsedTime / _duration))
                );
        }

        private float GetAlpha(double ratio)
        {
            const float slope = -1;
            const float exponent = 3;
            const float yOffset = 1;
            const float xOffset = 0;

            return slope * (float) Math.Pow(ratio - xOffset, exponent) + yOffset;
        }
    }
}
