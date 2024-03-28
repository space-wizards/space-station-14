using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking.Rules;

public sealed class SecretRuleSystem : GameRuleSystem<SecretRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    protected override void Added(EntityUid uid, SecretRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        PickRule(component);
    }

    protected override void Ended(EntityUid uid, SecretRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var rule in component.AdditionalGameRules)
        {
            GameTicker.EndGameRule(rule);
        }
    }

    private void PickRule(SecretRuleComponent component)
    {
        // TODO: This doesn't consider what can't start due to minimum player count,
        // but currently there's no way to know anyway as they use cvars.
        var presetString = _configurationManager.GetCVar(CCVars.SecretWeightPrototype);
        var preset = _prototypeManager.Index<WeightedRandomPrototype>(presetString).Pick(_random);
        Log.Info($"Selected {preset} for secret.");
        _adminLogger.Add(LogType.EventStarted, $"Selected {preset} for secret.");
        _chatManager.SendAdminAnnouncement(Loc.GetString("rule-secret-selected-preset", ("preset", preset)));

        var rules = _prototypeManager.Index<GamePresetPrototype>(preset).Rules;
        foreach (var rule in rules)
        {
            EntityUid ruleEnt;

            // if we're pre-round (i.e. will only be added)
            // then just add rules. if we're added in the middle of the round (or at any other point really)
            // then we want to start them as well
            if (GameTicker.RunLevel <= GameRunLevel.InRound)
                ruleEnt = GameTicker.AddGameRule(rule);
            else
            {
                GameTicker.StartGameRule(rule, out ruleEnt);
            }

            component.AdditionalGameRules.Add(ruleEnt);
        }
    }
}
