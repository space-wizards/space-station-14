using Content.Shared.Ninja.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedBatteryDrainerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryDrainerComponent, InteractionAttemptEvent>(OnInteract);
        SubscribeLocalEvent<BatteryDrainerComponent, DoAfterAttemptEvent<DrainDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<BatteryDrainerComponent, DrainDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Start do after for draining a power source.
    /// Can't predict PNBC existing so only done on server.
    /// </summary>
    protected virtual void OnInteract(EntityUid uid, BatteryDrainerComponent comp, InteractionAttemptEvent args)
    {
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
    public void SetBattery(BatteryDrainerComponent comp, EntityUid? battery)
    {
        comp.BatteryUid = battery;
    }
}
