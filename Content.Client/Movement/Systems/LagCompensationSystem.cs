using Content.Shared.Movement.Systems;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Client.Movement.Systems;

public sealed class LagCompensationSystem : SharedLagCompensationSystem
{
    public override (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(EntityUid uid, ICommonSession? pSession,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return (EntityCoordinates.Invalid, Angle.Zero);

        return (xform.Coordinates, xform.LocalRotation);
    }
}
