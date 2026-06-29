using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Used for implementing conditional logic for <see cref="EntityTableSelector"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class EntityTableCondition
{
    /// <summary>
    /// If true, inverts the result of the condition.
    /// </summary>
    [DataField]
    public bool Invert;

    public bool Evaluate(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        var res = EvaluateImplementation(root, entMan, proto, ctx);

        // XOR eval to invert the result.
        return res ^ Invert;
    }

    protected abstract bool EvaluateImplementation(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx);
}
