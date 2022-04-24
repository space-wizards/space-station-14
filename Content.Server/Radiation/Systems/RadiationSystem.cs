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
            // For now at least still need this because it uses a list internally then returns and this may be deleted before we get to it.
            // Update: Do we still need this?
            if ((!Exists(uid) ? EntityLifeStage.Deleted : MetaData(uid).EntityLifeStage) >= EntityLifeStage.Deleted)
                continue;

            IrradiateEntity(uid, radsPerSecond, time);
        }
    }

    public void IrradiateEntity(EntityUid uid, float radsPerSecond, float time)
    {
        var msg = new OnIrradiatedEvent(time, radsPerSecond);
        RaiseLocalEvent(uid, msg);
    }
}
