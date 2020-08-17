using System;
using System.Threading;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameTicking.GameRules
{
    /// <summary>
    ///     Simple GameRule that will do a free-for-all death match.
    ///     Kill everybody else to win.
    /// </summary>
    public sealed class RuleSuspicion : GameRule, IEntityEventSubscriber
    {
        private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(1);

#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IChatManager _chatManager;
        [Dependency] private readonly IGameTicker _gameTicker;
#pragma warning restore 649

        private readonly CancellationTokenSource _checkTimerCancel = new CancellationTokenSource();

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement("There are traitors on the station! Find them, and kill them!");

            Timer.SpawnRepeating(DeadCheckDelay, _checkWinConditions, _checkTimerCancel.Token);
        }

        public override void Removed()
        {
            base.Removed();

            _checkTimerCancel.Cancel();
        }

        private void _checkWinConditions()
        {
            var traitorsAlive = 0;
            var innocentsAlive = 0;

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

                if (playerSession.ContentData().Mind.HasRole<SuspicionTraitorRole>())
                    traitorsAlive++;
                else
                    innocentsAlive++;
            }

            if ((innocentsAlive + traitorsAlive) == 0)
            {
                _chatManager.DispatchServerAnnouncement("Everybody is dead, it's a stalemate!");
                EndRound();
            }

            else if (traitorsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement("The traitors are dead! The innocents win.");
                EndRound();
            }
            else if (innocentsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement("The innocents are dead! The traitors win.");
                EndRound();
            }
        }

        private void EndRound()
        {
            _gameTicker.EndRound();
            _chatManager.DispatchServerAnnouncement($"Restarting in 10 seconds.");
            _checkTimerCancel.Cancel();
            Timer.Spawn(TimeSpan.FromSeconds(10), () => _gameTicker.RestartRound());
        }
    }
}
