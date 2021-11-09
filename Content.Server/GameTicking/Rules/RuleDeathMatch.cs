using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules
{
    /// <summary>
    ///     Simple GameRule that will do a free-for-all death match.
    ///     Kill everybody else to win.
    /// </summary>
    public sealed class RuleDeathMatch : GameRule, IEntityEventSubscriber
    {
        private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(5);

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private CancellationTokenSource? _checkTimerCancel;

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-death-match-added-announcement"));

            _entityManager.EventBus.SubscribeEvent<DamageChangedEvent>(EventSource.Local, this, OnHealthChanged);
            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        }

        public override void Removed()
        {
            base.Removed();

            _entityManager.EventBus.UnsubscribeEvent<DamageChangedEvent>(EventSource.Local, this);
            _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
        }

        private void OnHealthChanged(DamageChangedEvent _)
        {
            _runDelayedCheck();
        }

        private void _checkForWinner()
        {
            _checkTimerCancel = null;

            if (!_cfg.GetCVar(CCVars.GameLobbyEnableWin))
                return;

            IPlayerSession? winner = null;
            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                var playerEntity = playerSession.AttachedEntity;
                if (playerEntity == null
                    || !playerEntity.TryGetComponent(out MobStateComponent? state))
                {
                    continue;
                }

                if (!state.IsAlive())
                {
                    continue;
                }

                if (winner != null)
                {
                    // Found a second person alive, nothing decided yet!
                    return;
                }

                winner = playerSession;
            }

            _chatManager.DispatchServerAnnouncement(winner == null
                ? Loc.GetString("rule-death-match-check-winner-stalemate")
                : Loc.GetString("rule-death-match-check-winner",("winner", winner)));

            var restartDelay = 10;

            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds", restartDelay)));

            Timer.Spawn(TimeSpan.FromSeconds(restartDelay), () => EntitySystem.Get<GameTicker>().RestartRound());
        }

        private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Disconnected)
            {
                _runDelayedCheck();
            }
        }

        private void _runDelayedCheck()
        {
            _checkTimerCancel?.Cancel();
            _checkTimerCancel = new CancellationTokenSource();

            Timer.Spawn(DeadCheckDelay, _checkForWinner, _checkTimerCancel.Token);
        }
    }
}
