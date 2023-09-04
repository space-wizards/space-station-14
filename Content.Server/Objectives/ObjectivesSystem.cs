using Content.Server.GameTicking;
﻿using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives;

public sealed class ObjectivesSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mind = default!;

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
        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var info = new ObjectivesTextGetInfoEvent(new List<EntityUid>(), string.Empty);
            RaiseLocalEvent(uid, ref info);
            if (info.Minds.Count == 0)
                continue;

            var agent = info.AgentName;
            var result = Loc.GetString("objectives-round-end-result", ("count", info.Minds.Count), ("agent", agent));
            var prepend = new ObjectivesTextPrependEvent(result);
            RaiseLocalEvent(uid, ref prepend);
            // space between the start text and player list
            result = prepend.Text + "\n";

            foreach (var mindId in info.Minds)
            {
                if (!TryComp(mindId, out MindComponent? mind))
                    continue;

                var name = mind.CharacterName;
                _mind.TryGetSession(mindId, out var session);
                var username = session?.Name;

                string title;
                if (username != null)
                {
                    if (name != null)
                        title = Loc.GetString("objectives-player-user-named", ("user", username), ("name", name));
                    else
                        title = Loc.GetString("objectives-player-user", ("user", username));
                }
                else
                {
                    // nothing to identify the player by, just give up
                    if (name == null)
                        continue;

                    title = Loc.GetString("objectives-player-named", ("name", name));
                }

                result += "\n";

                var objectives = mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    result += Loc.GetString("objectives-no-objectives", ("title", title), ("agent", agent));
                    continue;
                }

                result += Loc.GetString("objectives-with-objectives", ("title", title), ("agent", agent));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    result += "\n" + Loc.GetString($"objective-issuer-{objectiveGroup.Key}");

                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result += "\n- " + Loc.GetString(
                                    "objectives-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                );
                            }
                            else
                            {
                                result += "\n- " + Loc.GetString(
                                    "objectives-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                );
                            }
                        }
                    }
                }
            }

            ev.AddLine(result + "\n");
        }
    }

    public ObjectivePrototype? GetRandomObjective(EntityUid mindId, MindComponent mind, string objectiveGroupProto)
    {
        if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(objectiveGroupProto, out var groups))
        {
            Log.Error("Tried to get a random objective, but can't index WeightedRandomPrototype " + objectiveGroupProto);
            return null;
        }

        // TODO replace whatever the fuck this is with a proper objective selection system
        // yeah the old 'preventing infinite loops' thing wasn't super elegant either and it mislead people on what exactly it did
        var tries = 0;
        while (tries < 20)
        {
            var groupName = groups.Pick(_random);

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(groupName, out var group))
            {
                Log.Error("Couldn't index objective group prototype" + groupName);
                return null;
            }

            if (_prototypeManager.TryIndex<ObjectivePrototype>(group.Pick(_random), out var objective)
                && objective.CanBeAssigned(mindId, mind))
                return objective;
            else
                tries++;
        }

        return null;
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
public record struct ObjectivesTextGetInfoEvent(List<EntityUid> Minds, string AgentName);

/// <summary>
/// Raised on the game rule before text for each agent's objectives is added, letting you prepend something.
/// </summary>
[ByRefEvent]
public record struct ObjectivesTextPrependEvent(string Text);
