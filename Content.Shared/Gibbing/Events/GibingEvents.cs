namespace Content.Shared.Gibing.Events;

/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibbed</param>
/// <param name="GibbletCount">how many gibblets to spawn</param>
/// <param name="Canceled">Should we cancel gibbing</param>
public record struct PreEntityGibedEvent(EntityUid Target, int GibbletCount, bool Canceled = false);

/// <summary>
/// Called immediately after we gib the target entity
/// </summary>
/// <param name="Target">The entity being gibbed</param>
/// <param name="Giblets">The giblets we spawned (if any)</param>
/// <param name="SpilledEntities">Any entities that are spilled out (if any)</param>
public record struct EntityGibedEvent(EntityUid Target, List<EntityUid> Giblets, List<EntityUid> SpilledEntities);
