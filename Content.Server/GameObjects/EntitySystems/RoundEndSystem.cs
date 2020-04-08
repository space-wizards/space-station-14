using System;
using System.Threading;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.EntitySystems
{
    public class RoundEndSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private IGameTicker _gameTicker;
#pragma warning restore 649

        private CancellationTokenSource _roundEndCancellationTokenSource = new CancellationTokenSource();
        public bool IsRoundEndCountdownStarted { get; private set; }
        public int RoundEndCountdownTime { get; set; } = 5000;
        public DateTime? ExpectedCountdownEnd = null;

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

            ExpectedCountdownEnd = DateTime.Now.AddMilliseconds(RoundEndCountdownTime);
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
