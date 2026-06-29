using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.ValueSelector;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// A table of <see cref="EntProtoId"/>s with various configuration specifying how individual entries are selected.
/// </summary>
/// <seealso cref="EntityTableSystem"/>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
// Ideally this'd have `[Access(typeof(EntityTableSystem), typeof(IEntityTableVisitor)]`, but an interface with
// unspecified generics isn't a type, so it doesn't work. Plus I think it wouldn't allow implementations access...
public abstract partial class EntityTableSelector
{
    /// <summary>
    /// The number of times this selector is run
    /// </summary>
    [DataField]
    public NumberSelector Rolls = new ConstantNumberSelector(1);

    /// <summary>
    /// A weight used to pick between selectors.
    /// </summary>
    [DataField]
    public float Weight = 1;

    /// <summary>
    /// A simple chance that the selector will run.
    /// </summary>
    [DataField]
    public float Prob = 1;

    /// <summary>
    /// A list of conditions that must evaluate to 'true' for the selector to apply.
    /// </summary>
    [DataField]
    public List<EntityTableCondition> Conditions = new();

    /// <summary>
    /// If true, all the conditions must be successful in order for the selector to process.
    /// Otherwise, only one of them must be.
    /// </summary>
    [DataField]
    public bool RequireAll = true;

    /// <summary>
    /// Checks if this table should be rolled at all, based on <see cref="Conditions"/>.
    /// </summary>
    public bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        if (Conditions.Count == 0)
            return true;

        var success = false;
        foreach (var condition in Conditions)
        {
            var res = condition.Evaluate(this, entMan, proto, ctx);

            if (RequireAll && !res)
                return false; // intentional break out of loop and function

            success |= res;
        }

        return RequireAll || success;
    }

    /// <summary>
    /// Accepts <paramref name="visitor"/>, passing <paramref name="args"/> to it, and returning the result. Basically
    /// an alias for invoking <c>visitor.Visit(this, args)</c>.
    /// <br/>
    /// You almost definitely want to be calling something on <see cref="EntityTableSystem"/> instead.
    /// </summary>
    /// <seealso cref="IEntityTableVisitor{TArgs, TResult}"/>"/>
    [Access(Other = AccessPermissions.Execute)]
    public abstract TResult Accept<TArgs, TResult>(IEntityTableVisitor<TArgs, TResult> visitor, TArgs args);
}
