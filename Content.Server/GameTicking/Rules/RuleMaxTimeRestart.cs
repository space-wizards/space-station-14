using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules
{
    public sealed class RuleMaxTimeRestart : GameRule
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private CancellationTokenSource _timerCancel = new();

        public TimeSpan RoundMaxTime { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

        public override void Added()
        {
            base.Added();

            _entityManager.EventBus.SubscribeEvent<GameRunLevelChangedEvent>(EventSource.Local, this, RunLevelChanged);
        }

        public override void Removed()
        {
            base.Removed();

            _entityManager.EventBus.UnsubscribeEvents(this);
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
            EntitySystem.Get<GameTicker>().EndRound(Loc.GetString("Time has run out!"));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("Restarting in {0} seconds.", (int) RoundEndDelay.TotalSeconds));

            Timer.Spawn(RoundEndDelay, () => EntitySystem.Get<GameTicker>().RestartRound());
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
