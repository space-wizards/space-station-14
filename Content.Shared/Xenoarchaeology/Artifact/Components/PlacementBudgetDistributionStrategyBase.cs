using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

public abstract class PlacementBudgetDistributionStrategyBase
{
    public Dictionary<Enum, float> Distribute(
        float placementInBudget,
        IReadOnlyCollection<Enum> modifiers,
        IRobustRandom random
    )
    {
        if (modifiers.Count == 0)
            return new Dictionary<Enum, float>();

        if (placementInBudget == 0)
        {
            var result = new Dictionary<Enum, float>();
            foreach (var mod in modifiers)
            {
                result.Add(mod, 0);
            }

            return result;
        }

        return DistributeInternal(placementInBudget, modifiers, random);
    }

    protected abstract Dictionary<Enum, float> DistributeInternal(
        float placementInBudget,
        IReadOnlyCollection<Enum> modifiers,
        IRobustRandom random
    );
}