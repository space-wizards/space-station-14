using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

public sealed partial class IsNotRepeatingCondition : EntityTableCondition
{
    /// <summary>
    /// Context key used to track which entity prototypes have already been spawned
    /// while uniqueness checking is active.
    /// </summary>
    public const string UsedSpawnsKey = "UsedSpawns";


    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        if (!ctx.TryGetData<HashSet<EntProtoId>>(UsedSpawnsKey, out var used))
            return true;

        if (root is not EntSelector entSelector)
            return true;

        return !used.Contains(entSelector.Id);
    }
}
