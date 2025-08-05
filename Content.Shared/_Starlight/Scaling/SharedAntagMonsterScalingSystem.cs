using Content.Shared._Starlight.Scaling.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;

namespace Content.Shared._Starlight.Scaling;

public abstract partial class SharedScalingSystem : EntitySystem
{
    public void ApplyHealthScaling(
        EntityUid station,
        AntagMonsterScalingComponent scalingComp,
        MobThresholdsComponent thresholdsComp,
        Dictionary<EntityUid, double> cachedPopulations,
        double universalHealthWeight)
    {
        thresholdsComp.Thresholds.Clear();

        if (scalingComp.OriginalThresholds == null)
            return;

        foreach (var threshold in scalingComp.OriginalThresholds)
        {
            var key = threshold.Key;

            double scalingValue;
            if (cachedPopulations[station] < 0)
            {
                scalingValue = Math.Pow(universalHealthWeight, Math.Abs(cachedPopulations[station]));
            }
            else
            {
                scalingValue = Math.Pow(universalHealthWeight, cachedPopulations[station]);
            }

            FixedPoint2 scaledKey = key + (scalingValue * scalingComp.HealthScalingAdjustment);
            thresholdsComp.Thresholds.Add(scaledKey, threshold.Value);
        }
    }
}