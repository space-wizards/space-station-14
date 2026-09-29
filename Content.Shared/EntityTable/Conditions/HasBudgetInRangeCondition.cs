using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that will be successful only for entities that have
/// <see cref="XenoArtifactTriggerBudgetComponent"/> component with budget range
/// that contains budget value from context.
/// </summary>
public sealed partial class HasBudgetInRangeCondition : EntityTableCondition
{
    /// <summary>
    /// Key for <see cref="EntityTableContext"/> that can be used to hold current budget value,
    /// that should be compared against <see cref="XenoArtifactTriggerBudgetComponent.BudgetRange"/>.
    /// </summary>
    public const string BudgetContextKey = "Budget";

    /// <inheritdoc/>
    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        if (!ctx.TryGetData<float>(BudgetContextKey, out var budget))
            return false;


        if (root is not EntSelector entSelector)
            return false;

        if (proto.Index(entSelector.Id).TryComp(out XenoArtifactTriggerBudgetComponent? triggerBudgetRangeComponent, entMan.ComponentFactory))
        {
            return budget >= triggerBudgetRangeComponent.BudgetRange.Min && budget <= triggerBudgetRangeComponent.BudgetRange.Max;

        }

        if (proto.Index(entSelector.Id).TryComp(out XenoArtifactNodeBudgetComponent? nodeBudgetRangeComponent, entMan.ComponentFactory))
        {
            return budget >= nodeBudgetRangeComponent.BudgetRange.Min && budget <= nodeBudgetRangeComponent.BudgetRange.Max;

        }

        var log = Logger.GetSawmill("HasBudgetInRangeCondition");
        log.Error($"Rule {entSelector.Id} does not have a {nameof(XenoArtifactTriggerBudgetComponent)} or {nameof(XenoArtifactNodeBudgetComponent)}.");
        return false;

    }
}
