using Content.Server.Spawners.Components;

namespace Content.Server.Spawners.EntitySystems;

public sealed class TimedDespawnSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var entity in EntityQuery<TimedDespawnComponent>())
        {
            entity.Lifetime -= frameTime;

            if (entity.Lifetime <= 0)
                EntityManager.QueueDeleteEntity(entity.Owner);
        }
    }
}
