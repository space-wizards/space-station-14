using Content.Shared.Atmos;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Map;

namespace Content.Server.Radiation.Systems;

public sealed class RadiationSystem : SharedRadiationSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public void IrradiateRange(MapCoordinates epicenter, float range, float radsPerSecond, float time)
    {


        //var distance = Math.Abs(initialTile.X - current.X) + Math.Abs(initialTile.Y - initialTile.Y);
        //distance = distance <= 0 ? 1 : distance;

        /*var rads = radsPerSecond / distance * distance; // inverse square law
        if (rads < 0.1f)
            continue;


        visitNext.Enqueue(current.Offset(Direction.South));
        visitNext.Enqueue(current.Offset(Direction.East));
        visitNext.Enqueue(current.Offset(Direction.West));*/


    }

    public void IrradiateEntity(EntityUid uid, float radsPerSecond, float time)
    {
        var msg = new OnIrradiatedEvent(time, radsPerSecond);
        RaiseLocalEvent(uid, msg, true);
    }
}
