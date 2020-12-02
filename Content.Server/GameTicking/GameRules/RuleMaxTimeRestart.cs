using System;
using System.Threading;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameTicking.GameRules
{
    public sealed class RuleMaxTimeRestart : GameRule
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        private readonly CancellationTokenSource _timerCancel = new();

        public TimeSpan RoundMaxTime { get; set; }

        public override void Added()
        {
            base.Added();

            _gameTicker.OnRunLevelChanged += RunLevelChanged;
        }

        public override void Removed()
        {
            base.Removed();

            _gameTicker.OnRunLevelChanged -= RunLevelChanged;
            StopTimer();
        }

        public void RestartTimer()
        {
            _timerCancel.Cancel();
            Timer.Spawn(RoundMaxTime, TimerFired, _timerCancel.Token);
        }

        public void StopTimer()
        {
            _timerCancel.Cancel();
        }

        private void TimerFired()
        {
            _gameTicker.EndRound("Time has run out!");

            var restartDelay = 10;

            _chatManager.DispatchServerAnnouncement(Loc.GetString("Restarting in {0} seconds.", restartDelay));

            Timer.Spawn(TimeSpan.FromSeconds(restartDelay), () => _gameTicker.RestartRound());
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
    }
}
