namespace Content.Shared.Gibing.Events;

/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibletCount">how many giblets to spawn</param>
/// <param name="Canceled">Should we cancel gibing</param>
[ByRefEvent]
public record struct PreEntityGibedEvent(EntityUid Target, int GibletCount, bool Canceled = false);

/// <summary>
/// Called immediately after we gib the target entity
/// </summary>
/// <param name="Target">The entity being gibbed</param>
/// <param name="Giblets">The giblets we spawned (if any)</param>
/// <param name="DroppedEntities">Any entities that are spilled out (if any)</param>
[ByRefEvent]
public record struct EntityGibedEvent(EntityUid Target, List<EntityUid> Giblets, List<EntityUid> DroppedEntities);
