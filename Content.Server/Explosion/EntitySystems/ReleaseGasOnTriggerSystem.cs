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
        SubscribeLocalEvent<ReleaseGasOnTriggerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ReleaseGasOnTriggerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextReleaseTime = _timing.CurTime + ent.Comp.ReleaseInterval;
        ent.Comp.VolumeFraction = ent.Comp.Air.Volume / ent.Comp.ReleaseOverTimespan;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curtime = _timing.CurTime;

        var query = EntityQueryEnumerator<ReleaseGasOnTriggerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active || comp.NextReleaseTime > curtime)
                continue;

            var desiredVolume = comp.Air.Volume;

            if (comp.ReleaseOverTimespan != 0)
            {
                // Engage count dracula
                // Assume goal of 500 L over 5 seconds
                // 500 L / 5 s = 100 L/s
                // Multiply by dt, usually 0.5 s, so ex.
                // 100 L/s * 0.5 s = 50 L
                desiredVolume = comp.VolumeFraction * comp.ReleaseInterval.Seconds;
            }

            if (comp.FlowRateLimit != 0)
            {
                desiredVolume = Math.Min(desiredVolume, comp.FlowRateLimit);
            }

            // If we want 50 L and starting with 500 L:
            // 500 L - 50 L = 450 L
            // giverGasMix will get the remainder of 50 L which is what we want
            var giverGasMix = comp.Air.RemoveVolume(comp.Air.Volume - desiredVolume);

            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);
            if (environment != null)
            {
                _atmosphereSystem.Merge(environment, giverGasMix);
                comp.NextReleaseTime += comp.ReleaseInterval;
                if (comp.PressureLimit != 0 && environment.Pressure >= comp.PressureLimit ||
                    comp.Air.Volume <= 0)
                {
                    // Grenade did its job, and we don't want it sitting around any longer
                    // !basketball_skate.gif
                    QueueDel(uid);
                }

                continue;
            }

            // Middle finger the grenade because someone threw it into space
            QueueDel(uid);
        }
    }

    private void OnTrigger(Entity<ReleaseGasOnTriggerComponent> ent, ref TriggerEvent args)
    {
        // yeah
        ent.Comp.Active = true;
    }
}
