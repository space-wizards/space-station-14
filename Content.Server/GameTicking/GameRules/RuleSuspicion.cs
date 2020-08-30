using System;
using System.Threading;
using Content.Server.GameObjects.Components.Suspicion;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
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

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly CancellationTokenSource _checkTimerCancel = new CancellationTokenSource();

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement("There are traitors on the station! Find them, and kill them!");

            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Misc/tatoralert.ogg", AudioParams.Default,
                (session) => session.ContentData().Mind?.HasRole<SuspicionTraitorRole>() ?? false);

            EntitySystem.Get<DoorSystem>().AccessType = DoorSystem.AccessTypes.AllowAllNoExternal;

            Timer.SpawnRepeating(DeadCheckDelay, _checkWinConditions, _checkTimerCancel.Token);
        }

        public override void Removed()
        {
            base.Removed();

            EntitySystem.Get<DoorSystem>().AccessType = DoorSystem.AccessTypes.Id;

            _checkTimerCancel.Cancel();
        }

        private void _checkWinConditions()
        {
            if (!_cfg.GetCVar<bool>("game.enablewin"))
                return;

            var traitorsAlive = 0;
            var innocentsAlive = 0;

            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                if (playerSession.AttachedEntity == null
                    || !playerSession.AttachedEntity.TryGetComponent(out IDamageableComponent damageable)
                    || !playerSession.AttachedEntity.TryGetComponent(out SuspicionRoleComponent suspicionRole))
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
                EndRound(Victory.Stalemate);
            }

            else if (traitorsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement("The traitors are dead! The innocents win.");
                EndRound(Victory.Innocents);
            }
            else if (innocentsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement("The innocents are dead! The traitors win.");
                EndRound(Victory.Traitors);
            }
        }

        private enum Victory
        {
            Stalemate,
            Innocents,
            Traitors
        }

        private void EndRound(Victory victory)
        {
            string text;

            switch (victory)
            {
                case Victory.Innocents:
                    text = "The innocents have won!";
                    break;
                case Victory.Traitors:
                    text = "The traitors have won!";
                    break;
                default:
                    text = "Nobody wins!";
                    break;
            }

            _gameTicker.EndRound(text);
            _chatManager.DispatchServerAnnouncement($"Restarting in 10 seconds.");
            _checkTimerCancel.Cancel();
            Timer.Spawn(TimeSpan.FromSeconds(10), () => _gameTicker.RestartRound());
        }
    }
}
