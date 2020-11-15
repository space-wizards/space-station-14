using System;
using System.Threading;
using Content.Server.GameObjects.Components.Suspicion;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameTicking.GameRules
{
    public class RuleTraitor : GameRule
    {
        private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(1);

        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly CancellationTokenSource _checkTimerCancel = new CancellationTokenSource();

        public override void Added()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("Hello spessmen! Have a good shift!"));

            bool Predicate(IPlayerSession session) => session.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false;

            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Misc/tatoralert.ogg", AudioParams.Default, Predicate);

            //not sure if i should hook this into the healthchange entityevent or do it this way?
            Timer.SpawnRepeating(DeadCheckDelay, _checkWinConditions, _checkTimerCancel.Token);
        }

        public override void Removed()
        {
            base.Removed();

            _checkTimerCancel.Cancel();
        }

        private void _checkWinConditions()
        {
            if (!_cfg.GetCVar(CCVars.GameLobbyEnableWin))
                return;

            var traitorsAlive = 0;

            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                if (playerSession.AttachedEntity == null
                    || !playerSession.AttachedEntity.TryGetComponent(out IDamageableComponent damageable))
                {
                    continue;
                }

                if (damageable.CurrentState != DamageState.Alive)
                {
                    continue;
                }

                var mind = playerSession.ContentData()?.Mind;

                if (mind != null && mind.HasRole<TraitorRole>())
                    traitorsAlive++;
            }

            if (traitorsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("All traitors were killed! The crew wins!"));
                EndRound(GameEnd.TraitorsDead);
            }

            //todo: escaping
        }

        private enum GameEnd
        {
            Escaped,
            TraitorsDead
        }

        private void EndRound(GameEnd gameEnd)
        {
            string text;

            switch (gameEnd)
            {
                case GameEnd.Escaped:
                    text = Loc.GetString("The crew escaped!");
                    break;
                case GameEnd.TraitorsDead:
                    text = Loc.GetString("All traitors were killed!");
                    break;
                default:
                    text = Loc.GetString("Nobody wins!");
                    break;
            }

            _gameTicker.EndRound(text);

            var restartDelay = 10; //todo make longer

            //todo show gameendpanel

            _chatManager.DispatchServerAnnouncement(Loc.GetString("Restarting in {0} seconds.", restartDelay));
            _checkTimerCancel.Cancel();

            Timer.Spawn(TimeSpan.FromSeconds(restartDelay), () => _gameTicker.RestartRound());
        }

    }
}
