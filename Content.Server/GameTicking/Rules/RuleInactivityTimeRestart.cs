using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules
{
    public class RuleInactivityTimeRestart : GameRule
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private CancellationTokenSource _timerCancel = new();

        public TimeSpan InactivityMaxTime { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

        public override void Added()
        {
            base.Added();

            _entityManager.EventBus.SubscribeEvent<GameRunLevelChangedEvent>(EventSource.Local, this, RunLevelChanged);
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        public override void Removed()
        {
            base.Removed();

            _entityManager.EventBus.UnsubscribeEvents(this);
            _playerManager.PlayerStatusChanged -= PlayerStatusChanged;

            StopTimer();
        }

        public void RestartTimer()
        {
            _timerCancel.Cancel();
            _timerCancel = new CancellationTokenSource();
            Timer.Spawn(InactivityMaxTime, TimerFired, _timerCancel.Token);
        }

        public void StopTimer()
        {
            _timerCancel.Cancel();
        }

        private void TimerFired()
        {
            var gameticker = EntitySystem.Get<GameTicker>();
            gameticker.EndRound(Loc.GetString("rule-time-has-run-out"));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds",(int) RoundEndDelay.TotalSeconds)));

            Timer.Spawn(RoundEndDelay, () => gameticker.RestartRound());
        }

        private void RunLevelChanged(GameRunLevelChangedEvent args)
        {
            switch (args.New)
            {
                case GameRunLevel.InRound:
                    RestartTimer();
                    break;
                case GameRunLevel.PreRoundLobby:
                case GameRunLevel.PostRound:
                    StopTimer();
                    break;
            }
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (EntitySystem.Get<GameTicker>().RunLevel != GameRunLevel.InRound)
            {
                return;
            }

            if (_playerManager.PlayerCount == 0)
            {
                RestartTimer();
            }
            else
            {
                StopTimer();
            }
        }
    }
}
