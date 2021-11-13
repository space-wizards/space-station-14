using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.Doors;
using Content.Server.Players;
using Content.Server.Suspicion;
using Content.Server.Suspicion.EntitySystems;
using Content.Server.Suspicion.Roles;
using Content.Shared.CCVar;
using Content.Shared.MobState.Components;
using Content.Shared.Sound;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules
{
    /// <summary>
    ///     Simple GameRule that will do a TTT-like gamemode with traitors.
    /// </summary>
    public sealed class RuleSuspicion : GameRule, IEntityEventSubscriber
    {
        private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(1);

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        [DataField("addedSound")] private SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");

        private readonly CancellationTokenSource _checkTimerCancel = new();
        private TimeSpan _endTime;

        public TimeSpan RoundMaxTime { get; set; } = TimeSpan.FromSeconds(CCVars.SuspicionMaxTimeSeconds.DefaultValue);
        public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

        public override void Added()
        {
            RoundMaxTime = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.SuspicionMaxTimeSeconds));

            _endTime = _timing.CurTime + RoundMaxTime;

            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-added-announcement"));

            var filter = Filter.Empty()
                .AddWhere(session => ((IPlayerSession) session).ContentData()?.Mind?.HasRole<SuspicionTraitorRole>() ?? false);

            SoundSystem.Play(filter, _addedSound.GetSound(), AudioParams.Default);
            EntitySystem.Get<SuspicionEndTimerSystem>().EndTime = _endTime;

            EntitySystem.Get<DoorSystem>().AccessType = DoorSystem.AccessTypes.AllowAllNoExternal;

            Timer.SpawnRepeating(DeadCheckDelay, CheckWinConditions, _checkTimerCancel.Token);
        }

        public override void Removed()
        {
            base.Removed();

            EntitySystem.Get<DoorSystem>().AccessType = DoorSystem.AccessTypes.Id;
            EntitySystem.Get<SuspicionEndTimerSystem>().EndTime = null;

            _checkTimerCancel.Cancel();
        }

        private void Timeout()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-traitor-time-has-run-out"));

            EndRound(Victory.Innocents);
        }

        private void CheckWinConditions()
        {
            if (!_cfg.GetCVar(CCVars.GameLobbyEnableWin))
                return;

            var traitorsAlive = 0;
            var innocentsAlive = 0;

            foreach (var playerSession in _playerManager.GetAllPlayers())
            {
                if (playerSession.AttachedEntity == null
                    || !playerSession.AttachedEntity.TryGetComponent(out MobStateComponent? mobState)
                    || !playerSession.AttachedEntity.HasComponent<SuspicionRoleComponent>())
                {
                    continue;
                }

                if (!mobState.IsAlive())
                {
                    continue;
                }

                var mind = playerSession.ContentData()?.Mind;

                if (mind != null && mind.HasRole<SuspicionTraitorRole>())
                    traitorsAlive++;
                else
                    innocentsAlive++;
            }

            if (innocentsAlive + traitorsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-stalemate"));
                EndRound(Victory.Stalemate);
            }

            else if (traitorsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-station-win"));
                EndRound(Victory.Innocents);
            }
            else if (innocentsAlive == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-traitor-win"));
                EndRound(Victory.Traitors);
            }
            else if (_timing.CurTime > _endTime)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-traitor-time-has-run-out"));
                EndRound(Victory.Innocents);
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
                    text = Loc.GetString("rule-suspicion-end-round-innocents-victory");
                    break;
                case Victory.Traitors:
                    text = Loc.GetString("rule-suspicion-end-round-trators-victory");
                    break;
                default:
                    text = Loc.GetString("rule-suspicion-end-round-nobody-victory");
                    break;
            }

            var gameTicker = EntitySystem.Get<GameTicker>();
            gameTicker.EndRound(text);

            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds", (int) RoundEndDelay.TotalSeconds)));
            _checkTimerCancel.Cancel();

            Timer.Spawn(RoundEndDelay, () => gameTicker.RestartRound());
        }
    }
}
