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

        switch (ent.Comp.LockOnTrigger)
        {
            case LockAction.Lock:
                _lock.Lock(ent.Owner, args.User);
                break;
            case LockAction.Unlock:
                _lock.Unlock(ent, args.User);
                break;
            case LockAction.Toggle:
                _lock.ToggleLock(ent, args.User);
                break;
        }
    }
}
