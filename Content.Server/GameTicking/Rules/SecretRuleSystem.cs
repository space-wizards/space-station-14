using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public class SecretRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override string Prototype => "Secret";

    private readonly IReadOnlyDictionary<string, float> _ruleTable = new Dictionary<string, float>
    {
        { "Traitor", 0.75f },
    };

    private readonly List<string> _attachedRules = new();

    public override void Added()
    {
        PickRule();
    }

    public override void Removed()
    {
        foreach (var rule in _attachedRules)
        {
            _ticker.RemoveGameRule(_prototypeManager.Index<GameRulePrototype>(rule));
        }

        _attachedRules.Clear();
    }

    private void PickRule()
    {
        var table = _ruleTable.ToList();
        _random.Shuffle(table);
        foreach (var rule in table)
        {
            if (!_random.Prob(rule.Value))
                continue;

            if (_ticker.AddGameRule(_prototypeManager.Index<GameRulePrototype>(rule.Key)))
            {
                _attachedRules.Add(rule.Key);
                break;
            }
        }
    }
}
