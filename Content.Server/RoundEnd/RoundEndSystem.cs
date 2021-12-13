using System;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Database;
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
    public class RoundEndSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        [Dependency] private readonly AdminLogSystem _adminLog = default!;

        public const float RestartRoundTime = 20f;

        private CancellationTokenSource _roundEndCancellationTokenSource = new();
        private CancellationTokenSource _callCooldownEndedTokenSource = new();
        public bool IsRoundEndCountdownStarted { get; private set; }
        public TimeSpan RoundEndCountdownTime { get; set; } = TimeSpan.FromMinutes(4);
        public TimeSpan? ExpectedCountdownEnd = null;

        public TimeSpan LastCallTime { get; private set; }

        public TimeSpan CallCooldown { get; } = TimeSpan.FromSeconds(30);

        // TODO: Make these regular eventbus events...
        public delegate void RoundEndCountdownStarted();
        public event RoundEndCountdownStarted? OnRoundEndCountdownStarted;

        public delegate void RoundEndCountdownCancelled();
        public event RoundEndCountdownCancelled? OnRoundEndCountdownCancelled;

        public delegate void RoundEndCountdownFinished();
        public event RoundEndCountdownFinished? OnRoundEndCountdownFinished;

        public delegate void CallCooldownEnded();
        public event CallCooldownEnded? OnCallCooldownEnded;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        void Reset(RoundRestartCleanupEvent ev)
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

        public void RequestRoundEnd(EntityUid? requester = null, bool checkCooldown = true)
        {
            RequestRoundEnd(RoundEndCountdownTime, requester, checkCooldown);
        }

        public void RequestRoundEnd(TimeSpan countdownTime, EntityUid? requester = null, bool checkCooldown = true)
        {
            if (IsRoundEndCountdownStarted)
                return;

            if (checkCooldown && !CanCall())
            {
                return;
            }

            if (requester != null)
            {
                _adminLog.Add(LogType.ShuttleCalled, LogImpact.High, $"Shuttle called by {ToPrettyString(requester.Value):user}");
            }
            else
            {
                _adminLog.Add(LogType.ShuttleCalled, LogImpact.High, $"Shuttle called");
            }

            IsRoundEndCountdownStarted = true;

            _chatManager.DispatchStationAnnouncement(Loc.GetString("round-end-system-shuttle-called-announcement",("minutes", countdownTime.Minutes)), Loc.GetString("Station"), false);

            SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/shuttlecalled.ogg");

            ExpectedCountdownEnd = _gameTiming.CurTime + countdownTime;
            Timer.Spawn(countdownTime, EndRound, _roundEndCancellationTokenSource.Token);

            ActivateCooldown();

            OnRoundEndCountdownStarted?.Invoke();
        }

        public void CancelRoundEndCountdown(EntityUid? requester = null, bool checkCooldown = true)
        {
            if (!IsRoundEndCountdownStarted)
                return;

            if (checkCooldown && !CanCall())
            {
                return;
            }

            if (requester != null)
            {
                _adminLog.Add(LogType.ShuttleRecalled, LogImpact.High, $"Shuttle recalled by {ToPrettyString(requester.Value):user}");
            }
            else
            {
                _adminLog.Add(LogType.ShuttleRecalled, LogImpact.High, $"Shuttle recalled");
            }

            IsRoundEndCountdownStarted = false;

            _chatManager.DispatchStationAnnouncement(Loc.GetString("round-end-system-shuttle-recalled-announcement"), Loc.GetString("Station"), false);

            SoundSystem.Play(Filter.Broadcast(), "/Audio/Announcements/shuttlerecalled.ogg");

            _roundEndCancellationTokenSource.Cancel();
            _roundEndCancellationTokenSource = new CancellationTokenSource();

            ExpectedCountdownEnd = null;

            ActivateCooldown();

            OnRoundEndCountdownCancelled?.Invoke();
        }

        public void EndRound()
        {
            OnRoundEndCountdownFinished?.Invoke();
            var gameTicker = Get<GameTicker>();
            gameTicker.EndRound();

            _chatManager.DispatchServerAnnouncement(Loc.GetString("round-end-system-round-restart-eta-announcement", ("seconds", RestartRoundTime)));

            Timer.Spawn(TimeSpan.FromSeconds(RestartRoundTime), () => gameTicker.RestartRound(), CancellationToken.None);
        }
    }
}
