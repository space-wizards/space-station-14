using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

public sealed partial class MaxRuleOccurenceCondition : EntityTableCondition
{
    [DataField]
    public int Max = 1;

    [DataField]
    public EntProtoId? RuleOverride;

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (root is not EntSelector && RuleOverride == null)
            return false;

        var entSelector = root as EntSelector;

        string rule;
        if (RuleOverride != null)
        {
            rule = RuleOverride.Value;
        }
        else
        {
            rule = entSelector!.Id;
        }

        var gameTicker = entMan.System<SharedGameTicker>();

        return gameTicker.AllPreviousGameRules.Count(p => p.Item2 == rule) < Max;
    }
}
