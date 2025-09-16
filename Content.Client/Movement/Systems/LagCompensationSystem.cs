using Content.Shared.Movement.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class LagCompensationSystem : SharedLagCompensationSystem
{
    public override (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(Entity<TransformComponent?> ent, GameTick tick)
    {
        return Resolve(ent, ref ent.Comp) ? (ent.Comp.Coordinates, ent.Comp.LocalRotation) : default;
    }
}
