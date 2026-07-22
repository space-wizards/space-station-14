using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Destructible.Thresholds.Triggers;
using Robust.Shared.Utility;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingResilienceSystem : SharedChangelingResilienceSystem
{
    protected override void PreventGibbing(Entity<ChangelingResilienceComponent> ent)
    {
        if (!TryComp<DestructibleComponent>(ent, out var destructible))
            return;

        foreach (var threshold in destructible.Thresholds)
        {
            if (threshold.Trigger is not DamageTypeTrigger)
                continue;

            var behaviours = threshold.Behaviors.ShallowClone();

            foreach (var behavior in behaviours)
            {
                if (behavior is not GibBehavior)
                    continue;

                threshold.Behaviors.Remove(behavior);
            }
        }
    }
}
