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
        IPrototypeManager proto)
    {
        if (!CheckConditions(entMan, proto))
            yield break;

        var rolls = Rolls.Get(rand);
        for (var i = 0; i < rolls; i++)
        {
            if (!rand.Prob(Prob))
                continue;

            foreach (var spawn in GetSpawnsImplementation(rand, entMan, proto))
            {
                yield return spawn;
            }
        }
    }

    public bool CheckConditions(IEntityManager entMan, IPrototypeManager proto)
    {
        if (Conditions.Count == 0)
            return true;

        var success = false;
        foreach (var condition in Conditions)
        {
            var res = condition.Evaluate(entMan, proto);

            if (RequireAll && !res)
                return false; // intentional break out of loop and function

            success |= res;
        }

        if (RequireAll)
            return true;

        return success;
    }

    protected abstract IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto);
}
