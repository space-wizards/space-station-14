using Content.Shared.Radiation.Events;
using Robust.Shared.Map;

namespace Content.Server.Radiation.Systems;

public sealed class RadiationSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

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
