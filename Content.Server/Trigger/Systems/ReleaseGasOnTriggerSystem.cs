using Content.Server.Atmos.EntitySystems;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Trigger.Systems;

public sealed class ReleaseGasOnTriggerSystem : SharedReleaseGasOnTriggerSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ReleaseGasOnTriggerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active || comp.NextReleaseTime > curTime)
                continue;

            var giverGasMix = comp.Air.Remove(comp.StartingTotalMoles * comp.RemoveFraction);
            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);

            if (environment == null)
            {
                _appearance.SetData(uid, ReleaseGasOnTriggerVisuals.Key, false);
                RemCompDeferred<ReleaseGasOnTriggerComponent>(uid);
                continue;
            }

            _atmosphereSystem.Merge(environment, giverGasMix);
            comp.NextReleaseTime += comp.ReleaseInterval;

            if (comp.PressureLimit != 0 && environment.Pressure >= comp.PressureLimit ||
                comp.Air.TotalMoles <= 0)
            {
                _appearance.SetData(uid, ReleaseGasOnTriggerVisuals.Key, false);
                RemCompDeferred<ReleaseGasOnTriggerComponent>(uid);
            }
        }
    }
}
