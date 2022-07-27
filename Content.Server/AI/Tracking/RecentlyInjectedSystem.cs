namespace Content.Server.AI.Tracking
{
    public sealed class RecentlyInjectedSystem : EntitySystem
    {

        Queue<EntityUid> RemQueue = new();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var toRemove in RemQueue)
            {
                RemComp<RecentlyInjectedComponent>(toRemove);
            }
            RemQueue.Clear();
            foreach (var entity in EntityQuery<RecentlyInjectedComponent>())
            {
                entity.Accumulator += frameTime;
                if (entity.Accumulator < entity.RemoveTime.TotalSeconds)
                    continue;
                entity.Accumulator = 0;
                RemQueue.Enqueue(entity.Owner);
            }
        }
    }
}
