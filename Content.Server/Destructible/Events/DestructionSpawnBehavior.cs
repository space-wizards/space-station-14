namespace Content.Server.Destructible.Events;

/// <summary>
///     Raised on objects that were destroyed and also have <see cref="EntitySpawnBehavior"/> in <see cref="DestructibleComponent"/>
/// </summary>

[ByRefEvent]
public record struct DestructionSpawnBehavior(EntityUid Spawned);
