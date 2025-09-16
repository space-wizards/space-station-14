using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// This system can be used to get an entity's position some time into the past.
/// This can be used to check the apparent position that a client would have seen at the time that they were attempting
/// to interact with an entity.
/// </summary>
/// <remarks>
/// This assumes that the client was not actively predicting the position of the entity. E.g., you shouldn't use this
/// to get the position of the player's own entity.
/// </remarks>
public abstract class SharedLagCompensationSystem : EntitySystem
{
    /// <summary>
    /// Fetch the position of an entity at some specific tick.
    /// </summary>
    /// <remarks>
    /// To get the position as it would have been seen by a client, the given tick should correspond to the client's
    /// IClientGameTiming.LastRealTick
    /// </remarks>
    public abstract (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(Entity<TransformComponent?> ent, GameTick tick);

    public Angle GetAngle(Entity<TransformComponent?> ent, GameTick tick)
        => GetCoordinatesAngle(ent, tick).Angle;

    public EntityCoordinates GetCoordinates(Entity<TransformComponent?> ent, GameTick tick)
        => GetCoordinatesAngle(ent, tick).Coordinates;
}
