using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that succeeds only when the specified gamerule has been run under a certain amount of times
/// </summary>
/// <remarks>
/// This is meant to be attached directly to EntSelector. If it is not, then you'll need to specify what rule
/// is being used inside RuleOverride.
/// </remarks>
public sealed partial class MaxRuleOccurenceCondition : EntityTableCondition
{
    /// <summary>
    /// The maximum amount of times this rule can have already be run.
    /// </summary>
    [DataField]
    public int Max = 1;

    /// <summary>
    /// The rule that is being checked for occurrences.
    /// If null, it will use the value on the attached selector.
    /// </summary>
    [DataField]
    public EntProtoId? RuleOverride;

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        string rule;
        if (RuleOverride is { } ruleOverride)
        {
            rule = ruleOverride;
        }
        else
        {
            rule = root is EntSelector entSelector
                ? entSelector.Id
                : string.Empty;
        }

        if (rule == string.Empty)
            return false;

        var gameTicker = entMan.System<SharedGameTicker>();

        return gameTicker.AllPreviousGameRules.Count(p => p.Item2 == rule) < Max;
    }
}
