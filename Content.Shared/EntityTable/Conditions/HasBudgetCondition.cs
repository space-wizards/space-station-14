using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking.Rules;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

public sealed partial class HasBudgetCondition : EntityTableCondition
{
    public const string BudgetContextKey = "Budget";

    [DataField]
    public int? CostOverride;

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (!ctx.TryGetData<int>(BudgetContextKey, out var budget))
            return false;

        if (root is not EntSelector && CostOverride == null)
            return false;

        var entSelector = root as EntSelector;

        int cost;
        if (CostOverride != null)
        {
            cost = CostOverride.Value;
        }
        else
        {
            if (!proto.Index(entSelector!.Id).TryGetComponent(out DynamicRuleCostComponent? costComponent, entMan.ComponentFactory))
                return false;

            cost = costComponent.Cost;
        }

        return budget >= cost;
    }
}
