using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Storage;
using Content.Server.StationEvents.Components;
using System.Linq;

namespace Content.Server.GameTicking.Rules;


/// <summary>
/// Entities spawned with this component will try to add gamerules from the provided EntitySpawnEntry list.
/// This can be used to subset gamerules for balance, or to do something else like add it to a prototype if you want ninjas to start with meteors etc.
/// </summary>
public sealed class RandomRuleSystem : GameRuleSystem<RandomRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;

    protected override void Added(EntityUid uid, RandomRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        var selectedRules = EntitySpawnCollection.GetSpawns(component.SelectableGameRules, _random);

        if (component.MinRules > component.MaxRules)
        {
            Log.Warning("Minimum rules is more than Maximum rules!");
            return;
        }

        int ruleQuant = _random.Next(component.MinRules, component.MaxRules + 1); // the padding is required for expected result. Dont ask me why Next was implimented this way.

        if (selectedRules == null || selectedRules.Count == 0)
            return;

        _random.Shuffle(selectedRules);
        var nRules = selectedRules.Take(ruleQuant); // Does not allow duplicate selection, unless the EntitySpawnCollection had duplicate entries.

        var availableEvents = _event.AvailableEvents(); // handles the player counts and individual event restrictions, we need to do it each i incase it changes

        foreach (var rule in nRules)
        {
            var ruleEnt = new EntityUid();

            // If the station is already initialized, just start the rule, otherwise let that happen at the start of round.
            if (GameTicker.RunLevel <= GameRunLevel.InRound)
            {
                ruleEnt = GameTicker.AddGameRule(rule);
            }
            else
            {
                if (!_prototypeManager.TryIndex(rule, out var ruleProto))
                {
                    Log.Warning("The selected random rule is missing a prototype!");
                    continue;
                }

                if (ruleProto.TryGetComponent<StationEventComponent>(out _, _compFact))
                {
                    if (!availableEvents.ContainsKey(ruleProto))
                    {
                        Log.Warning("The selected random rule is not available!");
                        continue;
                    }
                    GameTicker.StartGameRule(rule, out ruleEnt);
                }
            }

            var str = Loc.GetString("station-event-system-run-event", ("eventName", ToPrettyString(ruleEnt)));
            _chatManager.SendAdminAlert(str);
            Log.Info(str);
        }
    }

}
