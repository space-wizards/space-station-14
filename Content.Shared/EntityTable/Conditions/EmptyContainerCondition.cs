using Content.Shared.Containers;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that passes when a container has nothing in it.
/// </summary>
public sealed partial class EmptyContainerCondition : EntityTableCondition
{
    public const string ContainerContextKey = "Container";

    /// <summary>
    /// If true the condition fails when no container was passed in from context.
    /// </summary>
    [DataField]
    public bool RequireContainer = true;

    protected override bool EvaluateImplementation(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        if (!ctx.TryGetData<BaseContainer>(ContainerContextKey, out var container))
            return !RequireContainer;

        return container.Count == 0;
    }
}
