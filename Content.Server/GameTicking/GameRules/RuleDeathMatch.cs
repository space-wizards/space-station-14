using System;
using System.Threading;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameTicking.GameRules
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
        [Dependency] private readonly IGameTicker _gameTicker = default!;

        private CancellationTokenSource _checkTimerCancel;

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement("The game is now a death match. Kill everybody else to win!");

            _entityManager.EventBus.SubscribeEvent<HealthChangedEventArgs>(EventSource.Local, this, OnHealthChanged);
            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        }

        public override void Removed()
        {
            base.Removed();

            _entityManager.EventBus.UnsubscribeEvent<HealthChangedEventArgs>(EventSource.Local, this);
            _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
        }

        private void OnHealthChanged(HealthChangedEventArgs message)
        {
            _runDelayedCheck();
        }

        private void _checkForWinner()
        {
            _checkTimerCancel = null;

            IPlayerSession winner = null;
            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                if (playerSession.AttachedEntity == null
                    || !playerSession.AttachedEntity.TryGetComponent(out IDamageableComponent damageable))
                {
                    continue;
                }

                if (damageable.CurrentDamageState != DamageState.Alive)
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

            if (winner == null)
            {
                _chatManager.DispatchServerAnnouncement("Everybody is dead, it's a stalemate!");
            }
            else
            {
                // We have a winner!
                _chatManager.DispatchServerAnnouncement($"{winner} wins the death match!");
            }

            _chatManager.DispatchServerAnnouncement($"Restarting in 10 seconds.");

            Timer.Spawn(TimeSpan.FromSeconds(10), () => _gameTicker.RestartRound());
        }

        private void PlayerManagerOnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
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
