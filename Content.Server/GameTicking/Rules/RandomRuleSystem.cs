using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Storage;
using Content.Server.StationEvents.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class RandomRuleSystem : GameRuleSystem<RandomRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;

    private string _ruleCompName = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, RandomRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        var selectedRules = EntitySpawnCollection.GetSpawns(component.SelectableGameRules, _random);

        Random rnd = new Random();
        int ruleQuant = rnd.Next(component.MinRules, component.MaxRules);

        for (int i = 0; i < ruleQuant; i++)
        {
            var availableEvents = _event.AvailableEvents(); // handles the player counts and individual event restrictions, we need to do it each i incase it changes
            string? slag = null;

            foreach (var rule in selectedRules)
            {
                if (selectedRules == null)
                    return;
                // If the station is already initialized, just start the rule, otherwise let that happen at the start of round.
                if (GameTicker.RunLevel <= GameRunLevel.InRound)
                {
                    GameTicker.AddGameRule(rule);
                }
                else
                {
                    _prototypeManager.TryIndex(rule, out var ruleEnt);
                    if (ruleEnt == null)
                    {
                        Log.Warning("The selected random rule is null!");
                        continue;
                    }

                    if (ruleEnt.TryGetComponent<StationEventComponent>(out _, _compFact))
                    {
                        if (!availableEvents.ContainsKey(ruleEnt))
                        {
                            Log.Warning("The selected random rule is not available!");
                            continue;
                        }
                        GameTicker.StartGameRule(rule);
                    }
                }
                slag = rule;
            }

            // prevents same gamerule twice in one run.
            if (!string.IsNullOrEmpty(slag))
            {
                selectedRules.RemoveAt(selectedRules.IndexOf(slag));
            }
        }
    }

}
