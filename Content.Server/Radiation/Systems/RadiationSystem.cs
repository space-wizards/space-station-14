using Content.Shared.Radiation.Events;
using Robust.Shared.Map;

namespace Content.Server.Radiation.Systems;

public sealed class RadiationSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const float RadiationCooldown = 1.0f;
    private float _accumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _accumulator += frameTime;

        while (_accumulator > RadiationCooldown)
        {
            _accumulator -= RadiationCooldown;

            // All code here runs effectively every RadiationCooldown seconds, so use that as the "frame time".
            foreach (var comp in EntityManager.EntityQuery<RadiationSourceComponent>())
            {
                var ent = comp.Owner;
                if (Deleted(ent))
                    continue;

                var cords = Transform(ent).MapPosition;
                IrradiateRange(cords, comp.Range, comp.RadsPerSecond, RadiationCooldown);
            }
        }
    }

    public void IrradiateRange(MapCoordinates coordinates, float range, float radsPerSecond, float time)
    {
        var lookUp = _lookup.GetEntitiesInRange(coordinates, range);
        foreach (var uid in lookUp)
        {
            if (Deleted(uid))
                continue;

            IrradiateEntity(uid, radsPerSecond, time);
        }
    }

    public void IrradiateEntity(EntityUid uid, float radsPerSecond, float time)
    {
        var msg = new OnIrradiatedEvent(time, radsPerSecond);
        RaiseLocalEvent(uid, msg, true);
    }
}
