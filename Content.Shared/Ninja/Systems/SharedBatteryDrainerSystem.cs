using Content.Shared.Ninja.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Basic draining prediction and API, all real logic is handled serverside.
/// </summary>
public abstract class SharedBatteryDrainerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryDrainerComponent, DoAfterAttemptEvent<DrainDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<BatteryDrainerComponent, DrainDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Cancel any drain doafters if the battery is removed or, on the server, gets filled.
    /// </summary>
    protected virtual void OnDoAfterAttempt(Entity<BatteryDrainerComponent> ent, ref DoAfterAttemptEvent<DrainDoAfterEvent> args)
    {
        if (ent.Comp.BatteryUid == null)
            args.Cancel();
    }

    /// <summary>
    /// Drain power from a power source (on server) and repeat if it succeeded.
    /// Client will predict always succeeding since power is serverside.
    /// </summary>
    private void OnDoAfter(Entity<BatteryDrainerComponent> ent, ref DrainDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not {} target)
            return;

        // repeat if there is still power to drain
        args.Repeat = TryDrainPower(ent, target);
    }

    /// <summary>
    /// Attempt to drain as much power as possible into the powercell.
    /// Client always predicts this as succeeding since power is serverside and it can only fail once, when the powercell is filled or the target is emptied.
    /// </summary>
    protected virtual bool TryDrainPower(Entity<BatteryDrainerComponent> ent, EntityUid target)
    {
        return true;
    }

    /// <summary>
    /// Sets the battery field on the drainer.
    /// </summary>
    public void SetBattery(Entity<BatteryDrainerComponent?> ent, EntityUid? battery)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.BatteryUid == battery)
            return;

        ent.Comp.BatteryUid = battery;
        Dirty(ent, ent.Comp);
    }
}

/// <summary>
/// DoAfter event for <see cref="BatteryDrainerComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DrainDoAfterEvent : SimpleDoAfterEvent;
