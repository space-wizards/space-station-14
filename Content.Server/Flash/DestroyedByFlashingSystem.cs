using Content.Server.Flash.Components;

namespace Content.Server.Flash;
public sealed class DestroyedByFlashingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DestroyedByFlashingComponent, FlashAttemptEvent>(OnFlashAttempt);
    }
    private void OnFlashAttempt(Entity<DestroyedByFlashingComponent> destroyed, ref FlashAttemptEvent args)
    {
        var uidXform = Transform(destroyed);

        Spawn(destroyed.Comp.RemoveEffect, uidXform.Coordinates);

        destroyed.Comp.LifeCount -= 1;
        if (destroyed.Comp.LifeCount <= 0)
            QueueDel(destroyed);
    }
}
