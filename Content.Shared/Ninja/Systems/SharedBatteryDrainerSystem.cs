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
    /// Cancel any drain doafters if the battery is removed or gets filled.
    /// </summary>
    protected virtual void OnDoAfterAttempt(EntityUid uid, BatteryDrainerComponent comp, DoAfterAttemptEvent<DrainDoAfterEvent> args)
    {
        if (comp.BatteryUid == null)
        {
            args.Cancel();
        }
    }

    /// <summary>
    /// Drain power from a power source (on server) and repeat if it succeeded.
    /// Client will predict always succeeding since power is serverside.
    /// </summary>
    private void OnDoAfter(EntityUid uid, BatteryDrainerComponent comp, DrainDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        // repeat if there is still power to drain
        args.Repeat = TryDrainPower(uid, comp, args.Target.Value);
    }

    /// <summary>
    /// Attempt to drain as much power as possible into the powercell.
    /// Client always predicts this as succeeding since power is serverside and it can only fail once, when the powercell is filled or the target is emptied.
    /// </summary>
    protected virtual bool TryDrainPower(EntityUid uid, BatteryDrainerComponent comp, EntityUid target)
    {
        return true;
    }

    /// <summary>
    /// Sets the battery field on the drainer.
    /// </summary>
    public void SetBattery(EntityUid uid, EntityUid? battery, BatteryDrainerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.BatteryUid = battery;
    }
}

/// <summary>
/// DoAfter event for <see cref="BatteryDrainerComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DrainDoAfterEvent : SimpleDoAfterEvent { }
