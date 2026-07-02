using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Destructible.Thresholds.Triggers;
using Robust.Shared.Utility;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingResilienceSystem : SharedChangelingResilienceSystem
{
    [Dependency] private RespiratorSystem _respirator = default!;

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

    protected override void HandleGasp(EntityUid ent)
    {
        if (!TryComp<RespiratorComponent>(ent, out var respirator))
            return;

        _respirator.Exhale((ent, respirator));
        _respirator.UpdateSaturation(ent, respirator.MaxSaturation, respirator);
    }
}
