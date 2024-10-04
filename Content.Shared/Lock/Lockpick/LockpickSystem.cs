using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Lock.Lockpick;

public sealed class LockpickSystem : EntitySystem
{
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<LockpickComponent, AfterInteractEvent>(LockpickUse);
        SubscribeLocalEvent<LockpickComponent, LockpickingDoAfterEvent>(LockpickDoAfter);
    }

    //Checks if target entity has a locked lock component
    //Starts a lockpicking DoAfter
    private void LockpickUse(Entity<LockpickComponent> ent, ref AfterInteractEvent args)
    {
        if (!HasComp<LockComponent>(args.Target) || !_lockSystem.IsLocked(args.Target.Value))
            return;

        if (args.CanReach)
            return;

        _audio.PlayPredicted(ent.Comp.StartSound, args.Target.Value, ent.Owner);

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.LockpickTime,
            new LockpickingDoAfterEvent(args.Target.Value),
            ent,
            ent,
            args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    //Send an unlock message to target entity if DoAfter goes well
    private void LockpickDoAfter(Entity<LockpickComponent> ent, ref LockpickingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        _lockSystem.Unlock(args.LockTarget, args.User);

        _audio.PlayPredicted(ent.Comp.EndSound, args.LockTarget, ent.Owner);

        return;
    }
}

[Serializable, NetSerializable]
public sealed partial class LockpickingDoAfterEvent : SimpleDoAfterEvent 
{
    [NonSerialized]
    public EntityUid LockTarget;  //target entity 

    public LockpickingDoAfterEvent(EntityUid target)
    {
        LockTarget = target;
    }
}
