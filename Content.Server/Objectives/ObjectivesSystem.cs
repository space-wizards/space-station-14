using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;
using Robust.Server.Player;
using Robust.Shared.Utility;

namespace Content.Server.Objectives;

public sealed class ObjectivesSystem : SharedObjectivesSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    /// <summary>
    /// Adds objective text for each game rule's players on round end.
    /// </summary>
    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        // go through each gamerule getting data for the roundend summary.
        var summaries = new Dictionary<string, Dictionary<string, List<(EntityUid, string)>>>();
        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var info = new ObjectivesTextGetInfoEvent(new List<(EntityUid, string)>(), string.Empty);
            RaiseLocalEvent(uid, ref info);
            if (info.Minds.Count == 0)
                continue;

            // first group the gamerules by their agents, for example 2 different dragons
            var agent = info.AgentName;
            if (!summaries.ContainsKey(agent))
                summaries[agent] = new Dictionary<string, List<(EntityUid, string)>>();

            var prepend = new ObjectivesTextPrependEvent("");
            RaiseLocalEvent(uid, ref prepend);

            // next group them by their prepended texts
            // for example with traitor rule, group them by the codewords they share
            var summary = summaries[agent];
            if (summary.ContainsKey(prepend.Text))
            {
                // same prepended text (usually empty) so combine them
                summary[prepend.Text].AddRange(info.Minds);
            }
            else
            {
                summary[prepend.Text] = info.Minds;
            }
        }

        // convert the data into summary text
        foreach (var (agent, summary) in summaries)
        {
            // first get the total number of players that were in these game rules combined
            var total = 0;
            var totalInCustody = 0;
            foreach (var (_, minds) in summary)
            {
                total += minds.Count;
                totalInCustody += minds.Where(pair => IsInCustody(pair.Item1)).Count();
            }

            var result = new StringBuilder();
            result.AppendLine(Loc.GetString("objectives-round-end-result", ("count", total), ("agent", agent)));
            if (agent == Loc.GetString("traitor-round-end-agent-name"))
            {
                result.AppendLine(Loc.GetString("objectives-round-end-result-in-custody", ("count", total), ("custody", totalInCustody), ("agent", agent)));
            }
            // next add all the players with its own prepended text
            foreach (var (prepend, minds) in summary)
            {
                if (prepend != string.Empty)
                    result.Append(prepend);

                // add space between the start text and player list
                result.AppendLine();

                AddSummary(result, agent, minds);
            }

            ev.AddLine(result.AppendLine().ToString());
        }
    }

    private void AddSummary(StringBuilder result, string agent, List<(EntityUid, string)> minds)
    {
        var agentSummaries = new List<(string summary, float successRate, int completedObjectives)>();

        foreach (var (mindId, name) in minds)
        {
            if (!TryComp<MindComponent>(mindId, out var mind))
                continue;

            var title = GetTitle((mindId, mind), name);
            var custody = IsInCustody(mindId, mind) ? Loc.GetString("objectives-in-custody") : string.Empty;

            var objectives = mind.Objectives;
            if (objectives.Count == 0)
            {
                agentSummaries.Add((Loc.GetString("objectives-no-objectives", ("custody", custody), ("title", title), ("agent", agent)), 0f, 0));
                continue;
            }

            var completedObjectives = 0;
            var totalObjectives = 0;
            var agentSummary = new StringBuilder();
            agentSummary.AppendLine(Loc.GetString("objectives-with-objectives", ("custody", custody), ("title", title), ("agent", agent)));

            foreach (var objectiveGroup in objectives.GroupBy(o => Comp<ObjectiveComponent>(o).LocIssuer))
            {
                //TO DO:
                //check for the right group here. Getting the target issuer is easy: objectiveGroup.Key
                //It should be compared to the type of the group's issuer.
                agentSummary.AppendLine(objectiveGroup.Key);

                foreach (var objective in objectiveGroup)
                {
                    var info = GetInfo(objective, mindId, mind);
                    if (info == null)
                        continue;

                    var objectiveTitle = info.Value.Title;
                    var progress = info.Value.Progress;
                    totalObjectives++;

                    agentSummary.Append("- ");
                    if (progress > 0.99f)
                    {
                        agentSummary.AppendLine(Loc.GetString(
                            "objectives-objective-success",
                            ("objective", objectiveTitle),
                            ("markupColor", "green")
                        ));
                        completedObjectives++;
                    }
                    else
                    {
                        agentSummary.AppendLine(Loc.GetString(
                            "objectives-objective-fail",
                            ("objective", objectiveTitle),
                            ("progress", (int) (progress * 100)),
                            ("markupColor", "red")
                        ));
                    }
                }
            }

            var successRate = totalObjectives > 0 ? (float) completedObjectives / totalObjectives : 0f;
            agentSummaries.Add((agentSummary.ToString(), successRate, completedObjectives));
        }

        var sortedAgents = agentSummaries.OrderByDescending(x => x.successRate)
                                       .ThenByDescending(x => x.completedObjectives);

        foreach (var (summary, _, _) in sortedAgents)
        {
            result.AppendLine(summary);
        }
    }

    public EntityUid? GetRandomObjective(EntityUid mindId, MindComponent mind, ProtoId<WeightedRandomPrototype> objectiveGroupProto, float maxDifficulty)
    {
        if (!_prototypeManager.TryIndex(objectiveGroupProto, out var groupsProto))
        {
            Log.Error($"Tried to get a random objective, but can't index WeightedRandomPrototype {objectiveGroupProto}");
            return null;
        }

        // Make a copy of the weights so we don't trash the prototype by removing entries
        var groups = groupsProto.Weights.ShallowClone();

        while (_random.TryPickAndTake(groups, out var groupName))
        {
            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(groupName, out var group))
            {
                Log.Error($"Couldn't index objective group prototype {groupName}");
                return null;
            }

            var objectives = group.Weights.ShallowClone();
            while (_random.TryPickAndTake(objectives, out var objectiveProto))
            {
                if (TryCreateObjective((mindId, mind), objectiveProto, out var objective)
                    && Comp<ObjectiveComponent>(objective.Value).Difficulty <= maxDifficulty)
                    return objective;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns whether a target is considered 'in custody' (cuffed on the shuttle).
    /// </summary>
    private bool IsInCustody(EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return false;

        // Ghosting will not save you
        bool originalEntityInCustody = false;
        EntityUid? originalEntity = GetEntity(mind.OriginalOwnedEntity);
        if (originalEntity.HasValue && originalEntity != mind.OwnedEntity)
        {
            originalEntityInCustody = TryComp<CuffableComponent>(originalEntity, out var origCuffed) && origCuffed.CuffedHandCount > 0
                   && _emergencyShuttle.IsTargetEscaping(originalEntity.Value);
        }

        return originalEntityInCustody || (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0
               && _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value));
    }

    /// <summary>
    /// Get the title for a player's mind used in round end.
    /// Pass in the original entity name which is shown alongside username.
    /// </summary>
    public string GetTitle(Entity<MindComponent?> mind, string name)
    {
        if (Resolve(mind, ref mind.Comp) &&
            mind.Comp.OriginalOwnerUserId != null &&
            _player.TryGetPlayerData(mind.Comp.OriginalOwnerUserId.Value, out var sessionData))
        {
            var username = sessionData.UserName;
            return Loc.GetString("objectives-player-user-named", ("user", username), ("name", name));
        }

        return Loc.GetString("objectives-player-named", ("name", name));
    }
}

/// <summary>
/// Raised on the game rule to get info for any objectives.
/// If its minds list is set then the players will have their objectives shown in the round end text.
/// AgentName is the generic name for a player in the list.
/// </summary>
/// <remarks>
/// The objectives system already checks if the game rule is added so you don't need to check that in this event's handler.
/// </remarks>
[ByRefEvent]
public record struct ObjectivesTextGetInfoEvent(List<(EntityUid, string)> Minds, string AgentName);

/// <summary>
/// Raised on the game rule before text for each agent's objectives is added, letting you prepend something.
/// </summary>
[ByRefEvent]
public record struct ObjectivesTextPrependEvent(string Text);
