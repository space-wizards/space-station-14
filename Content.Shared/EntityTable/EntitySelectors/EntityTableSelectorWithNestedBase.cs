using Content.Shared.EntityTable.Conditions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Base type for entity table selector that can have child selectors and properly apply entity table conditions for them.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class EntityTableSelectorWithChildrenBase : EntityTableSelectorWithNestedBase
{
    /// <summary>
    /// The child entries of this selector.
    /// </summary>
    [DataField(required: true)]
    public List<EntityTableSelector> Children = new();

    /// <inheritdoc/>>
    public override bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        if (!base.CheckConditions(entMan, proto, ctx))
            return false;

        foreach (var selector in Children)
        {
            // If any child succeeds this is a valid node
            if (selector.CheckConditions(entMan, proto, ctx))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Base type for entity table selector that can apply list of additional entity table conditions upon
/// nested entity table selectors.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class EntityTableSelectorWithNestedBase : EntityTableSelector
{
    /// <summary>
    /// A list of conditions that must evaluate to 'true' for the selector to apply.
    /// </summary>
    [DataField]
    public List<EntityTableCondition> ConditionsForChildren = new();

    /// <inheritdoc/>>
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
