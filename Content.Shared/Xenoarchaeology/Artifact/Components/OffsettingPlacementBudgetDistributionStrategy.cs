using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

public sealed class OffsettingPlacementBudgetDistributionStrategy : PlacementBudgetDistributionStrategyBase
{
    /// <inheritdoc />
    protected override Dictionary<Enum, float> DistributeInternal(float placementInBudget, IReadOnlyCollection<Enum> modifiers, IRobustRandom random)
    {
        var selected = random.Pick(modifiers);
        var mostPoints = placementInBudget * 0.7f;
        var othersShare = (placementInBudget * 0.3f) / (modifiers.Count - 1);
        var result = new Dictionary<Enum, float>();
        foreach (var mod in modifiers)
        {
            var share = Equals(mod, selected)
                ? mostPoints
                : othersShare;
            result.Add(mod, share);
        }

        return result;
    }
}