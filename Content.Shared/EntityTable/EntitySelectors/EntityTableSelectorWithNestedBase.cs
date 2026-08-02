using Content.Shared.EntityTable.Conditions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable.EntitySelectors;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class EntityTableSelectorWithChildrenBase : EntityTableSelectorWithNestedBase
{
    /// <summary>
    /// The child entries of this selector.
    /// </summary>
    [DataField(required: true)]
    public List<EntityTableSelector> Children = new();

    public override bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        var result = base.CheckConditions(entMan, proto, ctx);
        var nestedSuccess = false;
        foreach (var selector in Children)
        {
            nestedSuccess |= selector.CheckConditions(entMan, proto, ctx);
        }

        return result && nestedSuccess;
    }
}

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class EntityTableSelectorWithNestedBase : EntityTableSelector
{
    /// <summary>
    /// A list of conditions that must evaluate to 'true' for the selector to apply.
    /// </summary>
    [DataField]
    public List<EntityTableCondition> ConditionsForChildren = new();

    public override IEnumerable<EntProtoId> GetSpawns(IRobustRandom rand, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        var hasAdditionalConditions = ctx.TryGetData<List<EntityTableCondition>>(AdditionalConditionsKey, out var existingConditions);
        if (ConditionsForChildren.Count == 0 && !hasAdditionalConditions)
        {
            foreach (var spawn in base.GetSpawns(rand, entMan, proto, ctx))
            {
                yield return spawn;
            }

            yield break;
        }

        List<EntityTableCondition> conditionsToUse = new(ConditionsForChildren);
        if (hasAdditionalConditions)
        {
            conditionsToUse.AddRange(existingConditions!);
        }

        ctx.SetData(AdditionalConditionsKey, conditionsToUse);

        foreach (var spawn in base.GetSpawns(rand, entMan, proto, ctx))
        {
            yield return spawn;
        }
        // restore context after checks are done
        ctx.SetData(AdditionalConditionsKey, existingConditions!);
    }
}
