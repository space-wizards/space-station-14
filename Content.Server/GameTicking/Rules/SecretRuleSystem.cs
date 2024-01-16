using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.CCVar;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking.Rules;

public sealed class SecretRuleSystem : GameRuleSystem<SecretRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    protected override void Started(EntityUid uid, SecretRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
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
        Logger.InfoS("gamepreset", $"Selected {preset} for secret.");

        var rules = _prototypeManager.Index<GamePresetPrototype>(preset).Rules;
        foreach (var rule in rules)
        {
            GameTicker.StartGameRule(rule, out var ruleEnt);
            component.AdditionalGameRules.Add(ruleEnt);
        }
    }
}
