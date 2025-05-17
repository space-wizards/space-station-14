using Content.Server.Atmos.EntitySystems;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

/// <summary>
/// Releases a gas mixture to the atmosphere when triggered.
/// Can also release gas over a set timespan to prevent trolling people
/// with the instant-wall-of-pressure-inator.
/// </summary>
public sealed partial class ReleaseGasOnTriggerSystem : SharedReleaseGasOnTriggerSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReleaseGasOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    /// <summary>
    /// Shrimply sets the component to active when triggered, allowing it to release over time.
    /// </summary>
    private void OnTrigger(Entity<ReleaseGasOnTriggerComponent> ent, ref TriggerEvent args)
    {
        ent.Comp.Active = true;
        ent.Comp.NextReleaseTime = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ReleaseGasOnTriggerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active || comp.NextReleaseTime > curTime)
                continue;

            var fraction = comp.RemoveFraction ?? 1f;
            var giverGasMix = comp.Air.RemoveRatio(fraction);
            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);

            if (environment == null)
            {
                QueueDel(uid);
                continue;
            }

            _atmosphereSystem.Merge(environment, giverGasMix);
            comp.NextReleaseTime += comp.ReleaseInterval;

            if (comp.PressureLimit != 0 && environment.Pressure >= comp.PressureLimit ||
                comp.Air.TotalMoles <= 0)
            {
                QueueDel(uid);
                continue;
            }

            if (comp.ExponentialRise)
                UpdateExponentialRise(comp, fraction);
        }
    }

    /// <summary>
    /// Updates the RemoveFraction for exponential rise.
    /// </summary>
    private static void UpdateExponentialRise(ReleaseGasOnTriggerComponent comp, float baseFraction)
    {
        comp.TimesReleased++;
        comp.RemoveFraction = 1f - MathF.Pow(1f - baseFraction, comp.TimesReleased);
    }
}
