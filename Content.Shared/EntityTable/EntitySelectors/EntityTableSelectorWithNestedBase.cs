using Content.Shared.EntityTable.Conditions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

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

    /// <inheritdoc/>
    public override bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        if (!base.CheckConditions(entMan, proto, ctx))
            return false;

        using var scoped = ScopedConditions(ctx);

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
/// When making type, derived from this one, please remember to use <see cref="ScopedConditions"/> in your implementation of 
/// <see cref="EntityTableSelector.GetSpawnsImplementation"/>, and inside <see cref="EntityTableSelector.CheckConditions"/>
/// (if you override it).
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class EntityTableSelectorWithNestedBase : EntityTableSelector
{
    /// <summary>
    /// A list of conditions that must evaluate to 'true' for the selector to apply.
    /// </summary>
    [DataField]
    public List<EntityTableCondition> ConditionsForChildren = new();

    /// <summary>
    /// Temporarily injects <see cref="ConditionsForChildren"/> (merged with any conditions already
    /// scoped in <paramref name="ctx"/>) into the context so they are evaluated for every selector
    /// below this one. The context is restored when the returned handle is disposed.
    /// </summary>
    protected IDisposable ScopedConditions(EntityTableContext ctx)
    {
        if (ConditionsForChildren.Count == 0)
            return default(ScopedConditionsRestore);

        if (!ctx.TryGetData<List<EntityTableCondition>>(AdditionalConditionsKey, out var existingConditions))
        {
            // Nothing was scoped before: our own list can be used directly, no copy.
            ctx.SetData(AdditionalConditionsKey, ConditionsForChildren);
            return new ScopedConditionsRestore(ctx, null);
        }

        // Merge our conditions with whatever is already scoped in the context.
        var combined = new List<EntityTableCondition>(ConditionsForChildren);
        combined.AddRange(existingConditions);
        ctx.SetData(AdditionalConditionsKey, combined);
        return new ScopedConditionsRestore(ctx, existingConditions);
    }

    /// <summary>
    /// Restores the <see cref="EntityTableContext"/> after a scoped-conditions block.
    /// </summary>
    protected readonly struct ScopedConditionsRestore(
        EntityTableContext? ctx,
        List<EntityTableCondition>? existingConditions
    ) : IDisposable
    {
        public void Dispose()
        {
            if(ctx == null)
                return;

            if (existingConditions is null)
                ctx.RemoveData(AdditionalConditionsKey);
            else
                ctx.SetData(AdditionalConditionsKey, existingConditions);
        }
    }
}
