using Content.Server.Atmos.EntitySystems;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems;

/// <summary>
/// Releases a gas mixture to the atmosphere when triggered.
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
        SubscribeLocalEvent<ReleaseGasOnTriggerComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<ReleaseGasOnTriggerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.VolumeDivision = ent.Comp.Air.Volume - ent.Comp.Air.Volume / ent.Comp.ReleaseOverTimespan;
    }

    private void OnTrigger(Entity<ReleaseGasOnTriggerComponent> ent, ref TriggerEvent args)
    {
        // yeah
        ent.Comp.Active = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ReleaseGasOnTriggerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active)
                continue;

            if (comp.ReleaseOverTimespan != 0 && comp.NextRelease > currentTime)
                continue;

            // Get the total volume and do a series of restrictions depending on what's specified.
            var released = comp.Air.Volume;

            if (comp.ReleaseOverTimespan != 0)
            {
                released = comp.VolumeDivision;
                comp.NextRelease = currentTime + TimeSpan.FromSeconds(comp.ReleaseOverTimespan);
            }

            if (comp.FlowRateLimit != 0)
                released = Math.Min(released, comp.FlowRateLimit);


            var giverGasMix = comp.Air.RemoveVolume(released);

            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);
            if (environment != null)
            {
                _atmosphereSystem.Merge(environment, giverGasMix);
                if (comp.PressureLimit != 0 && environment.Pressure >= comp.PressureLimit)
                {
                    // !basketball_skate.gif
                    QueueDel(uid);
                }
            }
        }
    }


    private void OnMapInit(Entity<ReleaseGasOnTriggerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ReleaseOverTimespan != 0)
            ent.Comp.NextRelease = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.ReleaseOverTimespan);
    }
}
