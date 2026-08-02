using Content.Shared.EntityTable.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

public abstract partial class EntityTableSelectorWithNestedBase : EntityTableSelector
{
    /// <summary>
    /// A list of conditions that must evaluate to 'true' for the selector to apply.
    /// </summary>
    [DataField]
    public List<EntityTableCondition> ConditionsForChildren = new();

    public override bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        List<EntityTableCondition> combinedConditions = new(ConditionsForChildren);
        if (ctx.TryGetData<List<EntityTableCondition>>(AdditionalConditionsKey, out var existingConditions))
        {
            combinedConditions.AddRange(existingConditions);
        }
        else
        {
            ctx.TryAddData(AdditionalConditionsKey, combinedConditions);
        }
            
        return base.CheckConditions(entMan, proto, ctx);
    }
}
