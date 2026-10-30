namespace Content.Shared.Ghost;

/// <summary>
/// Raised on a spawned entity after they use a ghost role mob spawner.
/// </summary>
/// <param name="Spawner">The entity that spawned this.</param>
/// <param name="Spawned">The entity spawned.</param>
[ByRefEvent]
public readonly record struct GhostRoleSpawnerUsedEvent(EntityUid Spawner, EntityUid Spawned);
