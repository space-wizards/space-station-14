using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.GamePresets
{
    [GamePreset("traitor")]
    public class PresetTraitor : GamePreset
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string ModeTitle => "Traitor";

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
                _chatManager.DispatchServerAnnouncement($"Not enough players readied up for the game! There were {readyPlayers.Count} players readied up out of {MinPlayers} needed.");
                return false;
            }

            if (readyPlayers.Count == 0)
            {
                _chatManager.DispatchServerAnnouncement("No players readied up! Can't start Traitor.");
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
                var traitorRole = new TraitorRole(mind);
                if (mind == null)
                {
                    Logger.ErrorS("preset", "Failed getting mind for picked traitor.");
                    continue;
                }

                // creadth: we need to create uplink for the antag.
                // PDA should be in place already, so we just need to
                // initiate uplink account.
                var uplinkAccount = new UplinkAccount(mind.OwnedEntity.Uid, StartingBalance);
                var inventory = mind.OwnedEntity.GetComponent<InventoryComponent>();
                if (!inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent pdaItem))
                {
                    Logger.ErrorS("preset", "Failed getting pda for picked traitor.");
                    continue;
                }

                var pda = pdaItem.Owner;

                var pdaComponent = pda.GetComponent<PDAComponent>();
                if (pdaComponent.IdSlotEmpty)
                {
                    Logger.ErrorS("preset","PDA had no id for picked traitor");
                    continue;
                }

                mind.AddRole(traitorRole);
                _traitors.Add(traitorRole);
                pdaComponent.InitUplinkAccount(uplinkAccount);
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

            _gameTicker.AddGameRule<RuleTraitor>();
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
            var traitorCount = _traitors.Count;
            var result = Loc.GetString("There {0} {1} {2}.", Loc.GetPluralString("was", "were", traitorCount),
                traitorCount, Loc.GetPluralString("traitor", "traitors", traitorCount));
            foreach (var traitor in _traitors)
            {
                result += Loc.GetString("\n{0} was a traitor",traitor.Mind.Session.Name);
                var objectives = traitor.Mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    result += ".\n";
                    continue;
                }

                result += Loc.GetString(" and had the following objectives:");
                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    result += $"\n[color=#87cefa]{Loc.GetString(objectiveGroup.Key)}[/color]";
                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            result +=
                                Loc.GetString("\n- {0} | {1}", condition.Title, (progress > 0.99f ? $"[color=green]{Loc.GetString("Success!")}[/color]" : $"[color=red]{Loc.GetString("Failed!")}[/color] ({(int) (progress * 100)}%)"));
                        }
                    }
                }
            }

            return result;
        }
    }
}
