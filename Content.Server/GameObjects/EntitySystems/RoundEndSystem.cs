using System;
using System.Threading;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.EntitySystems
{
    public class RoundEndSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public const float RestartRoundTime = 20f;

        private CancellationTokenSource _roundEndCancellationTokenSource = new();
        public bool IsRoundEndCountdownStarted { get; private set; }
        public TimeSpan RoundEndCountdownTime { get; set; } = TimeSpan.FromMinutes(4);
        public TimeSpan? ExpectedCountdownEnd = null;

        public delegate void RoundEndCountdownStarted();
        public event RoundEndCountdownStarted OnRoundEndCountdownStarted;

        public delegate void RoundEndCountdownCancelled();
        public event RoundEndCountdownCancelled OnRoundEndCountdownCancelled;

        public delegate void RoundEndCountdownFinished();
        public event RoundEndCountdownFinished OnRoundEndCountdownFinished;

        void IResettingEntitySystem.Reset()
        {
            IsRoundEndCountdownStarted = false;
            _roundEndCancellationTokenSource.Cancel();
            _roundEndCancellationTokenSource = new CancellationTokenSource();
            ExpectedCountdownEnd = null;
        }

        public void RequestRoundEnd()
        {
            if (IsRoundEndCountdownStarted)
                return;

            IsRoundEndCountdownStarted = true;

            _chatManager.DispatchStationAnnouncement(Loc.GetString("An emergency shuttle has been sent. ETA: {0} minutes.", RoundEndCountdownTime.Minutes), Loc.GetString("Station"));

            Get<AudioSystem>().PlayGlobal("/Audio/Announcements/shuttlecalled.ogg");

            ExpectedCountdownEnd = _gameTiming.CurTime + RoundEndCountdownTime;
            Timer.Spawn(RoundEndCountdownTime, EndRound, _roundEndCancellationTokenSource.Token);
            OnRoundEndCountdownStarted?.Invoke();
        }

        public void CancelRoundEndCountdown()
        {
            if (!IsRoundEndCountdownStarted)
                return;

            IsRoundEndCountdownStarted = false;

            _chatManager.DispatchStationAnnouncement(Loc.GetString("The emergency shuttle has been recalled."), Loc.GetString("Station"));

            Get<AudioSystem>().PlayGlobal("/Audio/Announcements/shuttlerecalled.ogg");

            _roundEndCancellationTokenSource.Cancel();
            _roundEndCancellationTokenSource = new CancellationTokenSource();

            ExpectedCountdownEnd = null;

            OnRoundEndCountdownCancelled?.Invoke();
        }

        private void EndRound()
        {
            OnRoundEndCountdownFinished?.Invoke();
            _gameTicker.EndRound();

            _chatManager.DispatchServerAnnouncement(Loc.GetString("Restarting the round in {0} seconds...", RestartRoundTime));

            Timer.Spawn(TimeSpan.FromSeconds(RestartRoundTime), () => _gameTicker.RestartRound(), CancellationToken.None);
        }
    }
}
