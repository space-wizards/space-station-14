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
    public const string ContainerContextKey = "Container";

    /// <inheritdoc/>>
    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        if (!ctx.TryGetData<BaseContainer>(ContainerContextKey, out var container))
            return false;

        return container.Count == 0;
    }
}
