using Content.Shared.Flash;
using Content.Server.Flash;

namespace Content.Server.Flash;

public sealed class DestroyedByFlashingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DestroyedByFlashingComponent, FlashAttemptEvent>(OnFlashAttempt);
    }
    private void OnFlashAttempt(EntityUid uid, DestroyedByFlashingComponent component, FlashAttemptEvent args)
    {
        var uidXform = Transform(uid);

        Spawn(component.RemoveEffect, uidXform.Coordinates);

        component.LifeCount -= 1;
        if (component.LifeCount <= 0)
            QueueDel(uid);
    }
}
