using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Sound;
using Content.Shared.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules
{
    public class TraitorRuleSystem : GameRuleSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override string Prototype => "Traitor";

        private readonly SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");
        private readonly List<TraitorRole> _traitors = new ();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
            SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        }

        public override void Added()
        {
            // This seems silly, but I'll leave it.
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-traitor-added-announcement"));
        }

        public override void Removed()
        {
            _traitors.Clear();
        }

        private void OnStartAttempt(RoundStartAttemptEvent ev)
        {
            if (!Enabled)
                return;

            var minPlayers = _cfg.GetCVar(CCVars.TraitorMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
                ev.Cancel();
                return;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
                ev.Cancel();
                return;
            }
        }

        private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
        {
            if (!Enabled)
                return;

            var playersPerTraitor = _cfg.GetCVar(CCVars.TraitorPlayersPerTraitor);
            var maxTraitors = _cfg.GetCVar(CCVars.TraitorMaxTraitors);
            var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
            var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);
            var maxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
            var maxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);

            var list = new List<IPlayerSession>(ev.Players).Where(x =>
                x.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job {CanBeAntag: false}) ?? false
            ).ToList();

            var prefList = new List<IPlayerSession>();

            foreach (var player in list)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                {
                    continue;
                }
                var profile = ev.Profiles[player.UserId];
                if (profile.AntagPreferences.Contains("Traitor"))
                {
                    prefList.Add(player);
                }
            }

            var numTraitors = MathHelper.Clamp(ev.Players.Length / playersPerTraitor,
                1, maxTraitors);

            for (var i = 0; i < numTraitors; i++)
            {
                IPlayerSession traitor;
                if(prefList.Count < numTraitors)
                {
                    if (list.Count == 0)
                    {
                        Logger.InfoS("preset", "Insufficient ready players to fill up with traitors, stopping the selection.");
                        break;
                    }
                    traitor = _random.PickAndTake(list);
                    Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
                }
                else
                {
                    traitor = _random.PickAndTake(prefList);
                    list.Remove(traitor);
                    Logger.InfoS("preset", "Selected a preferred traitor.");
                }
                var mind = traitor.Data.ContentData()?.Mind;
                if (mind == null)
                {
                    Logger.ErrorS("preset", "Failed getting mind for picked traitor.");
                    continue;
                }

                // creadth: we need to create uplink for the antag.
                // PDA should be in place already, so we just need to
                // initiate uplink account.
                DebugTools.AssertNotNull(mind.OwnedEntity);

                var uplinkAccount = new UplinkAccount(startingBalance, mind.OwnedEntity!);
                var accounts = EntityManager.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
                accounts.AddNewAccount(uplinkAccount);

                if (!EntityManager.EntitySysManager.GetEntitySystem<UplinkSystem>()
                    .AddUplink(mind.OwnedEntity!.Value, uplinkAccount))
                    continue;

                var traitorRole = new TraitorRole(mind);
                mind.AddRole(traitorRole);
                _traitors.Add(traitorRole);
            }

            var adjectives = _prototypeManager.Index<DatasetPrototype>("adjectives").Values;
            var verbs = _prototypeManager.Index<DatasetPrototype>("verbs").Values;

            var codewordPool = adjectives.Concat(verbs).ToList();
            var finalCodewordCount = Math.Min(codewordCount, codewordPool.Count);
            var codewords = new string[finalCodewordCount];
            for (var i = 0; i < finalCodewordCount; i++)
            {
                codewords[i] = _random.PickAndTake(codewordPool);
            }

            foreach (var traitor in _traitors)
            {
                traitor.GreetTraitor(codewords);
            }

            SoundSystem.Play(Filter.Empty().AddPlayers(_traitors.Select(t => t.Mind.Session!)), _addedSound.GetSound(), AudioParams.Default);
        }
    }
}
