using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules
{
    public sealed class RuleMaxTimeRestart : GameRule
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        private CancellationTokenSource _timerCancel = new();

        public TimeSpan RoundMaxTime { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

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
            _timerCancel = new CancellationTokenSource();
            Timer.Spawn(RoundMaxTime, TimerFired, _timerCancel.Token);
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
    }
}
