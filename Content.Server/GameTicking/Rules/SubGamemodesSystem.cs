using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class SubGamemodesSystem : GameRuleSystem<SubGamemodesComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Added(EntityUid uid, SubGamemodesComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        var count = 0;
        foreach (var (id, prob) in comp.Rules)
        {
            if (!_random.Prob(prob))
                continue;

            GameTicker.AddGameRule(id);

            // if limited, stop rolling rules once the limit is reached
            if (count == comp.Limit)
                return;
        }
    }
}
