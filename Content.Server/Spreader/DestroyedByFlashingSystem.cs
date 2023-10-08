using Content.Shared.Flash;
using Content.Server.Flash;

namespace Content.Server.Spreader;

public sealed class DestroyedByFlashingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashableComponent, FlashAttemptEvent>(OnFlashAttempt);
    }
    private void OnFlashAttempt(EntityUid uid, FlashableComponent component, FlashAttemptEvent args)
    {
        if (!TryComp<DestroyedByFlashingComponent>(uid, out var destroyedBy))
            return;

        var uidXform = Transform(uid);

        Spawn(destroyedBy.RemoveEffect, uidXform.Coordinates);

        destroyedBy.LifeCount -= 1;
        if (destroyedBy.LifeCount <= 0)
            QueueDel(uid);
    }
}
