using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that will be successful only if budget value from context by <see cref="BudgetContextKey"/> key
/// will be between min and max of <see cref="XenoArtifactTriggerBudgetRangeComponent.BudgetRange"/> (borders included).
/// </summary>
public sealed partial class HasArtifactBudgetInRangeCondition : EntityTableCondition
{
    /// <summary>
    /// Key for <see cref="EntityTableContext"/> that can be used to hold current budget value,
    /// that should be compared against budget ranges.
    /// </summary>
    public static readonly EntityTableContextKey<float> BudgetContextKey = new("Budget");

    /// <inheritdoc/>
    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        if (root is not EntSelector entSelector)
            return false;

        if (!ctx.TryGetData(BudgetContextKey, out var budget))
            return false;

        MinMax? range = null;
        var entityPrototype = proto.Index(entSelector.Id);
        if(entityPrototype.TryComp(out XenoArtifactTriggerBudgetRangeComponent? triggerComp, entMan.ComponentFactory))
        {
            range = triggerComp.BudgetRange;
        }
        else if (entityPrototype.TryComp(out XenoArtifactNodeComponent? nodeComp, entMan.ComponentFactory))
        {
            range = nodeComp.BudgetRange;
        }

        return range.HasValue && budget >= range.Value.Min && budget <= range.Value.Max;
    }
}
