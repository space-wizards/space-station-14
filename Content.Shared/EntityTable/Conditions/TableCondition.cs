using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Used for implementing conditional logic for <see cref="EntityTableSelector"/>
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class TableCondition
{
    /// <summary>
    /// If true, inverts the result of the condition.
    /// </summary>
    [DataField]
    public bool Invert;

    public bool Evaluate(IEntityManager entMan, IPrototypeManager proto)
    {
        var res = EvaluateImplementation(entMan, proto);

        if (Invert)
            return !res;
        return res;
    }

    public abstract bool EvaluateImplementation(IEntityManager entMan, IPrototypeManager proto);
}
