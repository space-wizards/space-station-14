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

        if (scalingComp.OriginalThresholds == null)
            return;

        foreach (var threshold in scalingComp.OriginalThresholds)
        {
            var key = threshold.Key;

            var scalingPercent = cachedPopulations[station] * universalHealthWeight;

            if (scalingPercent > scalingComp.MaximumHealthScaling)
                scalingPercent = scalingComp.MaximumHealthScaling;

            if (scalingPercent < 0.0 - scalingComp.MaximumHealthScaling)
                scalingPercent = 0.0 - scalingComp.MaximumHealthScaling;

            FixedPoint2 scalingValue = key.Double() * scalingPercent;

            FixedPoint2 scaledKey = key + scalingValue;

            if (key != scaledKey)
            {
                thresholdsComp.Thresholds.Remove(key);
                thresholdsComp.Thresholds.Add(scaledKey, threshold.Value);
            }
        }
    }
}