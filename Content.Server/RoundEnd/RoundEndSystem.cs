using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.RoundEnd
{
    public class RoundEndSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public const float RestartRoundTime = 20f;

        private CancellationTokenSource _roundEndCancellationTokenSource = new();
        private CancellationTokenSource _callCooldownEndedTokenSource = new();
        public bool IsRoundEndCountdownStarted { get; private set; }
        public TimeSpan RoundEndCountdownTime { get; set; } = TimeSpan.FromMinutes(4);
        public TimeSpan? ExpectedCountdownEnd = null;

        public TimeSpan LastCallTime { get; private set; }

        public TimeSpan CallCooldown { get; } = TimeSpan.FromSeconds(30);

        public delegate void RoundEndCountdownStarted();
        public event RoundEndCountdownStarted? OnRoundEndCountdownStarted;

        public delegate void RoundEndCountdownCancelled();
        public event RoundEndCountdownCancelled? OnRoundEndCountdownCancelled;

        public delegate void RoundEndCountdownFinished();
        public event RoundEndCountdownFinished? OnRoundEndCountdownFinished;

        public delegate void CallCooldownEnded();
        public event CallCooldownEnded? OnCallCooldownEnded;

        void IResettingEntitySystem.Reset()
        {
            IsRoundEndCountdownStarted = false;
            _roundEndCancellationTokenSource.Cancel();
            _roundEndCancellationTokenSource = new CancellationTokenSource();
            _callCooldownEndedTokenSource.Cancel();
            _callCooldownEndedTokenSource = new CancellationTokenSource();
            ExpectedCountdownEnd = null;
            LastCallTime = default;
        }

        public bool CanCall()
        {
            return _gameTiming.CurTime >= LastCallTime + CallCooldown;
        }

        private void ActivateCooldown()
        {
            _callCooldownEndedTokenSource.Cancel();
            _callCooldownEndedTokenSource = new CancellationTokenSource();
            LastCallTime = _gameTiming.CurTime;
            Timer.Spawn(CallCooldown, () => OnCallCooldownEnded?.Invoke(), _callCooldownEndedTokenSource.Token);
        }

        public void RequestRoundEnd()
        {
            if (IsRoundEndCountdownStarted)
                return;

            if (!CanCall())
            {
                return;
            }

            IsRoundEndCountdownStarted = true;

            _chatManager.DispatchStationAnnouncement(Loc.GetString("An emergency shuttle has been sent. ETA: {0} minutes.", RoundEndCountdownTime.Minutes), Loc.GetString("Station"));

            SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/shuttlecalled.ogg");

            ExpectedCountdownEnd = _gameTiming.CurTime + RoundEndCountdownTime;
            Timer.Spawn(RoundEndCountdownTime, EndRound, _roundEndCancellationTokenSource.Token);

            ActivateCooldown();

            OnRoundEndCountdownStarted?.Invoke();
        }

        public void CancelRoundEndCountdown()
        {
            if (!IsRoundEndCountdownStarted)
                return;

            if (!CanCall())
            {
                return;
            }

            IsRoundEndCountdownStarted = false;

            _chatManager.DispatchStationAnnouncement(Loc.GetString("The emergency shuttle has been recalled."), Loc.GetString("Station"));

            SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/shuttlerecalled.ogg");

            _roundEndCancellationTokenSource.Cancel();
            _roundEndCancellationTokenSource = new CancellationTokenSource();

            ExpectedCountdownEnd = null;

            ActivateCooldown();

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
