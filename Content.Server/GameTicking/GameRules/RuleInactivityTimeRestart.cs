#nullable enable
using System;
using System.Threading;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.GameRules
{
    public class RuleInactivityTimeRestart : GameRule
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private CancellationTokenSource _timerCancel = new();

        public TimeSpan InactivityMaxTime { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

        public override void Added()
        {
            base.Added();

            _gameTicker.OnRunLevelChanged += RunLevelChanged;
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        public override void Removed()
        {
            base.Removed();

            _gameTicker.OnRunLevelChanged -= RunLevelChanged;
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
            _gameTicker.EndRound(Loc.GetString("Time has run out!"));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("Restarting in {0} seconds.", (int) RoundEndDelay.TotalSeconds));

            Timer.Spawn(RoundEndDelay, () => _gameTicker.RestartRound());
        }

        private void RunLevelChanged(GameRunLevelChangedEventArgs args)
        {
            switch (args.NewRunLevel)
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
            if (_gameTicker.RunLevel != GameRunLevel.InRound)
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
