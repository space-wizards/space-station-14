using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

public sealed class NormalPlacementBudgetDistributionStrategy : PlacementBudgetDistributionStrategyBase
{
    /// <inheritdoc />
    protected override Dictionary<Enum, float> DistributeInternal(float placementInBudget, IReadOnlyCollection<Enum> modifiers, IRobustRandom random)
    {
        var fairShare = placementInBudget / modifiers.Count;
        var result = new Dictionary<Enum, float>();
        foreach (var mod in modifiers)
        {
            result.Add(mod, fairShare);
        }

        return result;
    }
}