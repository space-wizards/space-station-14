using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Storage;
using System.Linq;

namespace Content.Server.GameTicking.Rules;


/// <summary>
/// Entities spawned with this component will try to add gamerules from the provided EntitySpawnEntry list.
/// This can be used to make events happen concurrently ex: if you want ninjas to start with meteors etc.
/// </summary>
public sealed class RandomRuleSystem : GameRuleSystem<RandomRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;

    protected override void Started(EntityUid uid, RandomRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        var selectedRules = EntitySpawnCollection.GetSpawns(component.SelectableGameRules, _random);

        int ruleQuant = component.MinMaxRules.Next(_random);

        if (selectedRules == null || selectedRules.Count == 0)
            return;

        _random.Shuffle(selectedRules);
        var nRules = selectedRules.Take(ruleQuant); // Does not allow duplicate selection, unless the EntitySpawnCollection had duplicate entries.

        var availableEvents = _event.AvailableEvents(); // handles the player counts and individual event restrictions, we need to do it each i incase it changes

        foreach (var rule in nRules)
        {
            if (!_prototypeManager.TryIndex(rule, out var ruleProto))
            {
                Log.Warning("The selected random rule is missing a prototype!");
                continue;
            }

            if (!availableEvents.ContainsKey(ruleProto))
            {
                Log.Warning("The selected random rule is not available!");
                continue;
            }
            GameTicker.StartGameRule(rule);
        }
    }

}
