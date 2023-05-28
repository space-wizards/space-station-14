using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Shared.Movement.Systems;

public abstract class SharedLagCompensationSystem : EntitySystem
{
    public abstract (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(EntityUid uid,
        ICommonSession? pSession,
        TransformComponent? xform = null);

    public Angle GetAngle(EntityUid uid, ICommonSession? session, TransformComponent? xform = null)
    {
        var (_, angle) = GetCoordinatesAngle(uid, session, xform);
        return angle;
    }

    public EntityCoordinates GetCoordinates(EntityUid uid, ICommonSession? session, TransformComponent? xform = null)
    {
        var (coordinates, _) = GetCoordinatesAngle(uid, session, xform);
        return coordinates;
    }
}
