namespace Content.Server.Delete;

public sealed class DeleteAfterTimeSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<DeleteAfterTimeComponent>())
        {
            comp.Accumulator += frameTime;
            if (comp.Accumulator < comp.DespawnTime.TotalSeconds)
                continue;

            QueueDel(comp.Owner);
        }
    }
}
