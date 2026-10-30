using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that passes when a container has nothing in it.
/// </summary>
public sealed partial class EmptyContainerCondition : EntityTableCondition
{
    /// <summary>
    /// Key for <see cref="EntityTableContext"/> to store container that should be checked by this condition.
    /// </summary>
    public static readonly EntityTableContextKey<BaseContainer> ContainerContextKey = new("Container");

    /// <inheritdoc/>>
    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        if (!ctx.TryGetData(ContainerContextKey, out var container))
            return false;

        return container.Count == 0;
    }
}
