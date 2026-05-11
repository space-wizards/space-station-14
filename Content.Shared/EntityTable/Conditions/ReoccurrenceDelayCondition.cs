using System.ComponentModel.DataAnnotations;
using System.Linq;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

public sealed partial class ReoccurrenceDelayCondition : EntityTableCondition
{
    /// <summary>
    /// The maximum amount of times this rule can have already be run.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.Zero;

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

        return gameTicker.AllPreviousGameRules.Any(
            p => p.Item2 == rule && p.Item1 + Delay <= gameTicker.RoundDuration());
    }
}
