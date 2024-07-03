using Content.Server.Temperature.Components;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureDamageSystem
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnParentChange(Entity<TemperatureDamageThresholdsComponent> entity, ref EntParentChangedMessage args)
    {
        // We only need to update thresholds if the thresholds changed for the entity's ancestors.
        var oldThresholds = args.OldParent != null
            ? RecalculateParentThresholds(args.OldParent.Value)
            : (null, null);

        var newThresholds = RecalculateParentThresholds(_xformQuery.GetComponent(entity).ParentUid);

        if (oldThresholds != newThresholds)
        {
            RecursiveThresholdUpdate(entity);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnParentThresholdStartup(Entity<ContainerTemperatureDamageThresholdsComponent> entity, ref ComponentStartup args)
    {
        RecursiveThresholdUpdate(entity);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnParentThresholdShutdown(Entity<ContainerTemperatureDamageThresholdsComponent> entity, ref ComponentShutdown args)
    {
        RecursiveThresholdUpdate(entity);
    }

    /// <summary>
    /// Recalculate and apply parent thresholds for the root entity and all its descendant.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="temperatureQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private void RecursiveThresholdUpdate(EntityUid root)
    {
        RecalculateAndApplyParentThresholds(root);

        var enumerator = Transform(root).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            RecursiveThresholdUpdate(child);
        }
    }

    /// <summary>
    /// Recalculate parent thresholds and apply them on the uid temperature component.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="temperatureQuery"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private void RecalculateAndApplyParentThresholds(EntityUid uid)
    {
        if (!_thresholdsQuery.TryGetComponent(uid, out var thresholds))
        {
            return;
        }

        var newThresholds = RecalculateParentThresholds(_xformQuery.GetComponent(uid).ParentUid);
        thresholds.ParentHeatDamageThreshold = newThresholds.Item1;
        thresholds.ParentColdDamageThreshold = newThresholds.Item2;
    }

    /// <summary>
    /// Recalculate Parent Heat/Cold DamageThreshold by recursively checking each ancestor and fetching the
    /// maximum HeatDamageThreshold and the minimum ColdDamageThreshold if any exists (aka the best value for each).
    /// </summary>
    /// <param name="initialParentUid"></param>
    /// <param name="transformQuery"></param>
    /// <param name="tempThresholdsQuery"></param>
    private (float?, float?) RecalculateParentThresholds(EntityUid initialParentUid)
    {
        // Recursively check parents for the best threshold available
        var parentUid = initialParentUid;
        float? newHeatThreshold = null;
        float? newColdThreshold = null;
        while (parentUid.IsValid())
        {
            if (_containerThresholdsQuery.TryGetComponent(parentUid, out var newThresholds))
            {
                if (newThresholds.HeatDamageThreshold != null)
                {
                    newHeatThreshold = Math.Max(newThresholds.HeatDamageThreshold.Value,
                        newHeatThreshold ?? 0);
                }

                if (newThresholds.ColdDamageThreshold != null)
                {
                    newColdThreshold = Math.Min(newThresholds.ColdDamageThreshold.Value,
                        newColdThreshold ?? float.MaxValue);
                }
            }

            parentUid = _xformQuery.GetComponent(parentUid).ParentUid;
        }

        return (newHeatThreshold, newColdThreshold);
    }
}
