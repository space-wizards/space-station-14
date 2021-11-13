using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.Objectives.Interfaces;
using Content.Server.PDA;
using Content.Server.Players;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Inventory;
using Content.Shared.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Presets
{
    [GamePreset("traitor")]
    public class PresetTraitor : GamePreset
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] protected readonly IEntityManager EntityManager = default!;

        public override string ModeTitle => Loc.GetString("traitor-title");

        private int MinPlayers { get; set; }
        private int PlayersPerTraitor { get; set; }
        private int MaxTraitors { get; set; }
        private int CodewordCount { get; set; }
        private int StartingBalance { get; set; }
        private float MaxDifficulty { get; set; }
        private int MaxPicks { get; set; }

        private readonly List<TraitorRole> _traitors = new ();

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            MinPlayers = _cfg.GetCVar(CCVars.TraitorMinPlayers);
            PlayersPerTraitor = _cfg.GetCVar(CCVars.TraitorPlayersPerTraitor);
            MaxTraitors = _cfg.GetCVar(CCVars.TraitorMaxTraitors);
            CodewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
            StartingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);
            MaxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
            MaxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);

            if (!force && readyPlayers.Count < MinPlayers)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-not-enough-ready-players", ("readyPlayersCount", readyPlayers.Count), ("minimumPlayers", MinPlayers)));
                return false;
            }

            if (readyPlayers.Count == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
                return false;
            }

            var list = new List<IPlayerSession>(readyPlayers);
            var prefList = new List<IPlayerSession>();

            foreach (var player in list)
            {
                if (!ReadyProfiles.ContainsKey(player.UserId))
                {
                    continue;
                }
                var profile = ReadyProfiles[player.UserId];
                if (profile.AntagPreferences.Contains("Traitor"))
                {
                    prefList.Add(player);
                }
            }

            var numTraitors = MathHelper.Clamp(readyPlayers.Count / PlayersPerTraitor,
                1, MaxTraitors);

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

                var uplinkAccount = new UplinkAccount(StartingBalance, mind.OwnedEntity!.Uid);
                var accounts = EntityManager.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
                accounts.AddNewAccount(uplinkAccount);

                if (!EntityManager.EntitySysManager.GetEntitySystem<UplinkSystem>()
                    .AddUplink(mind.OwnedEntity, uplinkAccount))
                    continue;

                var traitorRole = new TraitorRole(mind);
                mind.AddRole(traitorRole);
                _traitors.Add(traitorRole);
            }

            var adjectives = _prototypeManager.Index<DatasetPrototype>("adjectives").Values;
            var verbs = _prototypeManager.Index<DatasetPrototype>("verbs").Values;

            var codewordPool = adjectives.Concat(verbs).ToList();
            var finalCodewordCount = Math.Min(CodewordCount, codewordPool.Count);
            var codewords = new string[finalCodewordCount];
            for (var i = 0; i < finalCodewordCount; i++)
            {
                codewords[i] = _random.PickAndTake(codewordPool);
            }

            foreach (var traitor in _traitors)
            {
                traitor.GreetTraitor(codewords);
            }

            EntitySystem.Get<GameTicker>().AddGameRule<RuleTraitor>();
            return true;
        }

        public override void OnGameStarted()
        {
            var objectivesMgr = IoCManager.Resolve<IObjectivesManager>();
            foreach (var traitor in _traitors)
            {
                //give traitors their objectives
                var difficulty = 0f;
                for (var pick = 0; pick < MaxPicks && MaxDifficulty > difficulty; pick++)
                {
                    var objective = objectivesMgr.GetRandomObjective(traitor.Mind);
                    if (objective == null) continue;
                    if (traitor.Mind.TryAddObjective(objective))
                        difficulty += objective.Difficulty;
                }
            }
        }

        public override string GetRoundEndDescription()
        {
            var result = Loc.GetString("traitor-round-end-result", ("traitorCount", _traitors.Count));

            foreach (var traitor in _traitors)
            {
                var name = traitor.Mind.CharacterName;
                traitor.Mind.TryGetSession(out var session);
                var username = session?.Name;

                var objectives = traitor.Mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    if (username != null)
                    {
                        if (name == null)
                            result += "\n" + Loc.GetString("traitor-user-was-a-traitor", ("user", username));
                        else
                            result += "\n" + Loc.GetString("traitor-user-was-a-traitor-named", ("user", username), ("name", name));
                    }
                    else if (name != null)
                        result += "\n" + Loc.GetString("traitor-was-a-traitor-named", ("name", name));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives", ("user", username));
                    else
                        result += "\n" + Loc.GetString("traitor-user-was-a-traitor-with-objectives-named", ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("traitor-was-a-traitor-with-objectives-named", ("name", name));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    result += "\n" + Loc.GetString($"preset-traitor-objective-issuer-{objectiveGroup.Key}");

                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                );
                            }
                            else
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                );
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
