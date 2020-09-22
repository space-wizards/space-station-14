using System;
using System.Threading;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.EntitySystems
{
    public class RoundEndSystem : EntitySystem
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private CancellationTokenSource _roundEndCancellationTokenSource = new CancellationTokenSource();
        public bool IsRoundEndCountdownStarted { get; private set; }
        public TimeSpan RoundEndCountdownTime { get; set; } = TimeSpan.FromMinutes(4);
        public TimeSpan? ExpectedCountdownEnd = null;

        public delegate void RoundEndCountdownStarted();
        public event RoundEndCountdownStarted OnRoundEndCountdownStarted;

        public delegate void RoundEndCountdownCancelled();
        public event RoundEndCountdownCancelled OnRoundEndCountdownCancelled;

        public delegate void RoundEndCountdownFinished();
        public event RoundEndCountdownFinished OnRoundEndCountdownFinished;

        public void RequestRoundEnd()
        {
            if (IsRoundEndCountdownStarted)
                return;

            IsRoundEndCountdownStarted = true;

            ExpectedCountdownEnd = _gameTiming.CurTime + RoundEndCountdownTime;
            Timer.Spawn(RoundEndCountdownTime, EndRound, _roundEndCancellationTokenSource.Token);
            OnRoundEndCountdownStarted?.Invoke();
        }

        public void CancelRoundEndCountdown()
        {
            if (!IsRoundEndCountdownStarted)
                return;

            IsRoundEndCountdownStarted = false;

            _roundEndCancellationTokenSource.Cancel();
            _roundEndCancellationTokenSource = new CancellationTokenSource();

            ExpectedCountdownEnd = null;

            OnRoundEndCountdownCancelled?.Invoke();
        }

        private void EndRound()
        {
            OnRoundEndCountdownFinished?.Invoke();
            _gameTicker.EndRound();
        }
    }
}
