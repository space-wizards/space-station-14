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
    /// Shrimply sets the component to active when triggered, allowing it to
    /// release over time.
    /// </summary>
    private void OnTrigger(Entity<ReleaseGasOnTriggerComponent> ent, ref TriggerEvent args)
    {
        // yeah
        ent.Comp.Active = true;
        // If the grenade marinates forever, it'll have its next release time
        // an hour back, which causes it to release instantly.
        // So we set it when it becomes active.
        ent.Comp.NextReleaseTime = _timing.CurTime;
    }

    /// <summary>
    /// Releases gas to the exposed atmosphere, depending on a series of set parameters.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curtime = _timing.CurTime;

        var query = EntityQueryEnumerator<ReleaseGasOnTriggerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active || comp.NextReleaseTime > curtime)
                continue;

            var fraction = comp.RemoveFraction ?? 1f;

            var giverGasMix = comp.Air.RemoveRatio(fraction);

            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);
            if (environment != null)
            {
                _atmosphereSystem.Merge(environment, giverGasMix);

                comp.NextReleaseTime += comp.ReleaseInterval;

                if (comp.PressureLimit != 0 && environment.Pressure >= comp.PressureLimit ||
                    comp.Air.TotalMoles <= 0)
                {
                    // Grenade did its job, and we don't want it sitting around any longer
                    // !basketball_skate.gif
                    QueueDel(uid);
                }

                if (comp.ExponentialRise)
                {
                    comp.TimesReleased++;
                    // Because we're ratioing the gas mixture, we need to increase the amount of gas
                    // we take per run otherwise the grenade sits there forever.

                    // Exponential rise: fraction = 1 - (1 - baseFraction) ^ n, where n is the number of releases so far.
                    // n = Time since activation / release interval
                    var newFraction = 1f - MathF.Pow(1f - fraction, comp.TimesReleased);
                    comp.RemoveFraction = newFraction;
                }

                continue;
            }

            // Middle finger the grenade because someone threw it into space
            QueueDel(uid);
        }
    }
}
