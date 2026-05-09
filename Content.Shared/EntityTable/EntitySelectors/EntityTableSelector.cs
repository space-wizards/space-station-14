using System.Linq;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.ValueSelector;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable.EntitySelectors;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
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
    public double Prob = 1;

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

    public IEnumerable<EntProtoId> GetSpawns(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (!CheckConditions(entMan, proto, ctx))
            yield break;

        var rolls = Rolls.Get(rand);
        for (var i = 0; i < rolls; i++)
        {
            if (!rand.Prob(Prob))
                continue;

            foreach (var spawn in GetSpawnsImplementation(rand, entMan, proto, ctx))
            {
                yield return spawn;
            }
        }
    }

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

        if (RequireAll)
            return true;

        return success;
    }

    /// <summary>
    /// Gets a list of every spawn in the table, and the odds of that spawn occuring, ignoring conditions.
    /// </summary>
    public IEnumerable<(EntProtoId spawn, double prob)> ListSpawns(IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx,
        float mod = 1f)
    {
        foreach (var (spawn, prob) in ListSpawnsImplementation(entMan, proto, ctx))
        {
            yield return (spawn, prob * Prob * Rolls.Odds() * mod);
        }
    }

    /// <summary>
    /// Gets a list of every spawn in the table, and the average number of occurrences, ignoring conditions.
    /// </summary>
    public IEnumerable<(EntProtoId spawn, double prob)> AverageSpawns(IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx,
        float mod = 1f)
    {
        foreach (var (spawn, prob) in AverageSpawnsImplementation(entMan, proto, ctx))
        {
            yield return (spawn, prob * Prob * Rolls.Average() * mod);
        }
    }

    protected abstract IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx);

    protected abstract IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx);

    protected abstract IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx);
}
