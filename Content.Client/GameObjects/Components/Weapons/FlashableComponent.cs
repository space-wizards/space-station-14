using System;
using System.Threading;
using Content.Shared.GameObjects.Components.Weapons;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components.Weapons
{
    [RegisterComponent]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
        private CancellationTokenSource _cancelToken;
        private TimeSpan _startTime;
        private double _duration;
        private FlashOverlay _overlay;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
            {
                return;
            }

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            if (playerManager?.LocalPlayer != null && playerManager.LocalPlayer.ControlledEntity != Owner)
            {
                return;
            }

            var newState = (FlashComponentState) curState;
            if (newState.Time == default)
            {
                return;
            }

            // Few things here:
            // 1. If a shorter duration flash is applied then don't do anything
            // 2. If the client-side time is later than when the flash should've ended don't do anything
            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;
            var newEndTime = newState.Time.TotalSeconds + newState.Duration;
            var currentEndTime = _startTime.TotalSeconds + _duration;

            if (currentEndTime > newEndTime)
            {
                return;
            }

            if (currentTime > newEndTime)
            {
                DisableOverlay();
                return;
            }

            _startTime = newState.Time;
            _duration = newState.Duration;

            EnableOverlay(newEndTime - currentTime);
        }

        private void EnableOverlay(double duration)
        {
            // If the timer gets reset
            if (_overlay != null)
            {
                _overlay.Duration = _duration;
                _overlay.StartTime = _startTime;
                _cancelToken.Cancel();
            }
            else
            {
                var overlayManager = IoCManager.Resolve<IOverlayManager>();
                _overlay = new FlashOverlay(_duration);
                overlayManager.AddOverlay(_overlay);
            }

            _cancelToken = new CancellationTokenSource();
            Owner.SpawnTimer((int) duration * 1000, DisableOverlay, _cancelToken.Token);
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
        private readonly IGameTiming _timer;
        private readonly IClyde _displayManager;
        public TimeSpan StartTime { get; set; }
        public double Duration { get; set; }
        public FlashOverlay(double duration) : base(nameof(FlashOverlay))
        {
            _timer = IoCManager.Resolve<IGameTiming>();
            _displayManager = IoCManager.Resolve<IClyde>();
            StartTime = _timer.CurTime;
            Duration = duration;
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            var elapsedTime = (_timer.CurTime - StartTime).TotalSeconds;
            if (elapsedTime > Duration)
            {
                return;
            }
            var screenHandle = (DrawingHandleScreen) handle;

            screenHandle.DrawRect(
                new UIBox2(0.0f, 0.0f, _displayManager.ScreenSize.X, _displayManager.ScreenSize.Y),
                Color.White.WithAlpha(GetAlpha(elapsedTime / Duration))
                );
        }

        private float GetAlpha(double ratio)
        {
            // Ideally you just want a smooth slope to finish it so it's not jarring at the end
            // By all means put in a better curve
            const float slope = -9.0f;
            const float exponent = 0.1f;
            const float yOffset = 9.0f;
            const float xOffset = 0.0f;

            // Overkill but easy to adjust if you want to mess around with the design
            var result = (float) MathHelper.Clamp(slope * (float) Math.Pow(ratio - xOffset, exponent) + yOffset, 0.0, 1.0);
            DebugTools.Assert(!float.IsNaN(result));
            return result;
        }
    }
}
