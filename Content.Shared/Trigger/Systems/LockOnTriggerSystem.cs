using Content.Shared.Lock;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class LockOnTriggerSystem : XOnTriggerSystem<LockOnTriggerComponent>
{
    [Dependency] private readonly LockSystem _lock = default!;

    protected override void OnTrigger(Entity<LockOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!TryComp<LockComponent>(target, out var lockComp))
            return; // prevent the Resolve in Lock/Unlock/ToggleLock from logging errors in case the user does not have the component

        switch (ent.Comp.LockMode)
        {
            case LockAction.Lock:
                _lock.Lock(target, args.User, lockComp);
                break;
            case LockAction.Unlock:
                _lock.Unlock(target, args.User, lockComp);
                break;
            case LockAction.Toggle:
                _lock.ToggleLock(target, args.User, lockComp);
                break;
        }
    }
}
