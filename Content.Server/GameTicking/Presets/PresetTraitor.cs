using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
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
    [GamePresetPrototype("traitor")]
    public class PresetTraitor : GamePresetPrototype
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


        // TODO: Move this over.
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

        // TODO: Move this over.
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
