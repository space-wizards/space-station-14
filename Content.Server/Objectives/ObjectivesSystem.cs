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
using Content.Server.Objectives.Commands;
using Content.Shared.Humanoid; //imp addition
using Content.Shared.CCVar;
using Content.Shared.Prototypes;
using Content.Shared.Roles; //imp addition
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server.Objectives;

public sealed class ObjectivesSystem : SharedObjectivesSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private IEnumerable<string>? _objectives;

    private bool _showGreentext;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);

        Subs.CVar(_cfg, CCVars.GameShowGreentext, value => _showGreentext = value, true);

        _prototypeManager.PrototypesReloaded += CreateCompletions;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _prototypeManager.PrototypesReloaded -= CreateCompletions;
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
                result.AppendLine(Loc.GetString("objectives-round-end-result-in-custody",
                    ("count", total),
                    ("custody", totalInCustody),
                    ("agent", agent)));
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
                agentSummaries.Add((
                    Loc.GetString("objectives-no-objectives", ("custody", custody), ("title", title), ("agent", agent)),
                    0f, 0));
                continue;
            }

            //imp edit - track which objectives are non-trivial so that you can't cheese a Wow!
            var completedNonTrivial = 0;
            var totalNontrivial = 0;
            //imp edit end

            var completedObjectives = 0;
            var totalObjectives = 0;
            var agentSummary = new StringBuilder();
            agentSummary.AppendLine(Loc.GetString("objectives-with-objectives",
                ("custody", custody),
                ("title", title),
                ("agent", agent)));

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
                    if (!info.Value.Trivial) //imp edit
                        totalNontrivial++;

                    agentSummary.Append("- ");
                    if (!_showGreentext)
                    {
                        agentSummary.AppendLine(objectiveTitle);
                    }
                    else if (progress > 0.99f)
                    {
                        agentSummary.AppendLine(Loc.GetString(
                            "objectives-objective-success",
                            ("objective", objectiveTitle),
                            ("markupColor", "green")
                        ));
                        completedObjectives++;
                        if (!info.Value.Trivial) //imp edit
                            completedNonTrivial++;
                    }
                    else
                    {
                        agentSummary.AppendLine(Loc.GetString(
                            "objectives-objective-fail",
                            ("objective", objectiveTitle),
                            ("progress", (int)(progress * 100)),
                            ("markupColor", "red")
                        ));
                    }
                }
            }

            //imp edit start - list the amount of currency this person spent & what they bought
            //if they bought nothing, check if they completed their objectives
            //so many loops........

            //these todos are future maybe fixes, not super necessary
            //todo figure out a way to get the starting balance of a given antag
            //todo figure out a way to fix the double-objective-summary thingimajig

            //get the character's gender
            var genderString = "epicene"; //default to they/them'ing people
            if (TryComp<HumanoidAppearanceComponent>(GetEntity(mind.OriginalOwnedEntity!), out var appearance))
            {
                genderString = appearance.Gender.ToString().ToLowerInvariant();
            }

            var nonTrivialSuccessRate = totalNontrivial > 0 ? (float)completedNonTrivial / totalNontrivial : 0f;
            foreach (var mindRole in mind.MindRoles)
            {
                if (!TryComp<MindRoleComponent>(mindRole, out var roleComp)) //sanity checking
                    continue;

                if (roleComp.Purchases.Count > 0)
                {
                    var costs = new Dictionary<string, int>(); //the total costs
                    var purchaseCounts = new Dictionary<string, int>(); //how many times a given thing was bought
                    foreach (var purchase in roleComp.Purchases)
                    {
                        //get how much was spent
                        foreach (var key in purchase.Item2.Keys)
                        {
                            var currencyName = _prototypeManager.Index(key).DisplayName;
                            if (!costs.TryGetValue(currencyName, out var cost))
                            {
                                cost = 0;
                            }

                            costs[currencyName] = cost + purchase.Item2[key].Int();
                        }

                        //get the amount of times each entry was bought
                        if (!purchaseCounts.TryGetValue(purchase.Item1, out var purchaseCount))
                        {
                            purchaseCount = 0;
                        }

                        purchaseCounts[purchase.Item1] = purchaseCount + 1;
                    }

                    var index = 0;
                    agentSummary.Append(Loc.GetString("roundend-spend-summary-spent", ("gender", genderString)) + " ");
                    //list totals spent
                    //hardcoding english grammar into this probably isn't great but I don't think fluent can do lists?
                    foreach (var costPair in costs) //technically can just get index 0 of the list because it should always have only 1 entry, but let's be safe
                    {
                        index++;
                        //if this is the last entry, do a full stop.
                        if (index == costs.Count)
                        {
                            agentSummary.AppendLine(costPair.Value + " " + Loc.GetString(costPair.Key) +
                                                    "."); //appendLine as this is the last entry
                            continue; //continue early for sanity
                        }

                        //if this is the second to last entry, use an & instead of a comma
                        if (index == costs.Count)
                        {
                            agentSummary.Append(costPair.Value + " " + Loc.GetString(costPair.Key) + " & ");
                            continue; // continue early for sanity
                        }

                        //finally, just do the entry with a comma

                        agentSummary.Append(costPair.Value + " " + Loc.GetString(costPair.Key) + ", ");
                    }

                    index = 0; //reset index

                    //list things bought
                    agentSummary.Append(Loc.GetString("roundend-spend-summary-bought", ("gender", genderString)) + " ");
                    foreach (var boughtThing in purchaseCounts)
                    {
                        index++;
                        //if this is the last entry, do a full stop.
                        if (index == purchaseCounts.Count)
                        {
                            agentSummary.AppendLine(boughtThing.Value + "x " + Loc.GetString(boughtThing.Key) + "."); //appendLine as this is the last entry
                            continue; //continue early for sanity
                        }

                        //if this is the second to last entry, use an & instead of a comma
                        if (index == purchaseCounts.Count - 1)
                        {
                            agentSummary.Append(boughtThing.Value + "x " + Loc.GetString(boughtThing.Key) + " & ");
                            continue; // continue early for sanity
                        }

                        //finally, just do the entry with a comma
                        agentSummary.Append(boughtThing.Value + "x " + Loc.GetString(boughtThing.Key) + ", ");
                    }
                }
                else if (roleComp.GetsNoSpendtext)
                {
                    agentSummary.AppendLine(nonTrivialSuccessRate >= 0.5f
                        ? Loc.GetString("roundend-spent-nothing-success")
                        : Loc.GetString("roundend-spent-nothing-failure", ("gender", genderString)));
                }
            }
            //imp edit end

            var successRate = totalObjectives > 0 ? (float)completedObjectives / totalObjectives : 0f;
            agentSummaries.Add((agentSummary.ToString(), successRate, completedObjectives));
        }

        var sortedAgents = agentSummaries.OrderByDescending(x => x.successRate)
            .ThenByDescending(x => x.completedObjectives);

        foreach (var (summary, _, _) in sortedAgents)
        {
            result.AppendLine(summary);
        }
    }

    public EntityUid? GetRandomObjective(EntityUid mindId,
        MindComponent mind,
        ProtoId<WeightedRandomPrototype> objectiveGroupProto,
        float maxDifficulty)
    {
        if (!_prototypeManager.TryIndex(objectiveGroupProto, out var groupsProto))
        {
            Log.Error(
                $"Tried to get a random objective, but can't index WeightedRandomPrototype {objectiveGroupProto}");
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
            originalEntityInCustody = TryComp<CuffableComponent>(originalEntity, out var origCuffed) &&
                                      origCuffed.CuffedHandCount > 0
                                      && _emergencyShuttle.IsTargetEscaping(originalEntity.Value);
        }

        return originalEntityInCustody || (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) &&
                                           cuffed.CuffedHandCount > 0
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

            var nameWithJobMaybe = name;
            if (_job.MindTryGetJobName(mind, out var jobName))
                nameWithJobMaybe += ", " + jobName;

            return Loc.GetString("objectives-player-user-named", ("user", username), ("name", nameWithJobMaybe));
        }

        return Loc.GetString("objectives-player-named", ("name", name));
    }


    private void CreateCompletions(PrototypesReloadedEventArgs unused)
    {
        CreateCompletions();
    }

    /// <summary>
    /// Get all objective prototypes by their IDs.
    /// This is used for completions in <see cref="AddObjectiveCommand"/>
    /// </summary>
    public IEnumerable<string> Objectives()
    {
        if (_objectives == null)
            CreateCompletions();

        return _objectives!;
    }

    private void CreateCompletions()
    {
        _objectives = _prototypeManager.EnumeratePrototypes<EntityPrototype>()
            .Where(p => p.HasComponent<ObjectiveComponent>())
            .Select(p => p.ID)
            .Order();
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
