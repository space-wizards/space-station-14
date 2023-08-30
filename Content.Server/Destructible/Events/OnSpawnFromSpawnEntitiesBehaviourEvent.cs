namespace Content.Server.Destructible.Events;

/// <summary>
///     Raised on objects that were spawned because of <see cref="EntitySpawnBehavior"/> from <see cref="DestructibleSystem"/>
/// </summary>
public sealed class OnSpawnFromSpawnEntitiesBehaviourEvent : EntityEventArgs
{
    public OnSpawnFromSpawnEntitiesBehaviourEvent(EntityUid spawned)
    {
        Spawned = spawned;
    }

    public EntityUid Spawned { get; }
}
