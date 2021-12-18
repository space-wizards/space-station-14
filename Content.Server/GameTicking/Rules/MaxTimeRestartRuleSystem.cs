using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules
{
    public sealed class MaxTimeRestartRuleSystem : GameRuleSystem
    {
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override string Prototype => "MaxTimeRestart";

        private CancellationTokenSource _timerCancel = new();

        public TimeSpan RoundMaxTime { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

        public override void Added()
        {
            EntityManager.EventBus.SubscribeEvent<GameRunLevelChangedEvent>(EventSource.Local, this, RunLevelChanged);
        }

        public override void Removed()
        {
            EntityManager.EventBus.UnsubscribeEvents(this);

            RoundMaxTime = TimeSpan.FromMinutes(5);
            RoundEndDelay = TimeSpan.FromMinutes(10);

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
            _gameTicker.EndRound(Loc.GetString("rule-time-has-run-out"));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds",("seconds", (int) RoundEndDelay.TotalSeconds)));

            Timer.Spawn(RoundEndDelay, () => _gameTicker.RestartRound());
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
    }
}
