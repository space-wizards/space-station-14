using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

public sealed class AllInOnePlacementBudgetDistributionStrategy : PlacementBudgetDistributionStrategyBase
{
    /// <inheritdoc />
    protected override Dictionary<Enum, float> DistributeInternal(float placementInBudget, IReadOnlyCollection<Enum> modifiers, IRobustRandom random)
    {
        var selected = random.Pick(modifiers);
        var result = new Dictionary<Enum, float>();
        foreach (var mod in modifiers)
        {
            var share = Equals(mod, selected)
                ? placementInBudget
                : 0;

            result.Add(mod, share);
        }

        return result;
    }
}