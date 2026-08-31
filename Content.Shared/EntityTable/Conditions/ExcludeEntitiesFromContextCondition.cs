using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition for checking that selector is not going to spawn entity that is marked in current context as excluded .
/// Can be used to keep spawns unique (if no selector rolls more than 1).
/// </summary>
public sealed partial class ExcludeEntitiesFromContextCondition : EntityTableCondition
{
    /// <summary>
    /// Context key used to track which entity prototypes should not be spawned.
    /// </summary>
    public const string EntitiesToExclude = "EntitiesToExclude";

    /// <inheritdoc/>>
    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        if (!ctx.TryGetData<HashSet<EntProtoId>>(EntitiesToExclude, out var used))
            return true;

        if (root is not EntSelector entSelector)
            return true;

        return !used.Contains(entSelector.Id);
    }
}
