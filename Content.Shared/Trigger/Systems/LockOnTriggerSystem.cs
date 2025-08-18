using Content.Shared.Lock;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class LockOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<LockOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (!TryComp<LockComponent>(target, out var lockComp))
            return; // prevent the Resolve in Lock/Unlock/ToggleLock from logging errors in case the user does not have the component

        switch (ent.Comp.LockMode)
        {
            case LockAction.Lock:
                _lock.Lock(target.Value, args.User, lockComp);
                break;
            case LockAction.Unlock:
                _lock.Unlock(target.Value, args.User, lockComp);
                break;
            case LockAction.Toggle:
                _lock.ToggleLock(target.Value, args.User, lockComp);
                break;
        }
    }
}
