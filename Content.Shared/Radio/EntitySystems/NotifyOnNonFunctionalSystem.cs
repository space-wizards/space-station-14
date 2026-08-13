using Content.Shared.Construction;
using Content.Shared.Destructible;
using Content.Shared.Lock;
using Content.Shared.Radio.Components;

namespace Content.Shared.Radio.EntitySystems;

public abstract partial class SharedNotifyOnNonFunctionalSystem : EntitySystem
{

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<NotifyOnNonFunctionalComponent> ent, ref DestructionEventArgs args)
    {
        // Engineering needs to know if an emitter is destroyed so they can replace it before the engine looses.
        AlertRadio(ent, ent.Comp.LocDestroyed);
    }

    // you shouldn't be able to deconstruct locked emitters but out of scope to fix
    [SubscribeLocalEvent]
    private void OnDeconstructed(Entity<NotifyOnNonFunctionalComponent> ent, ref MachineDeconstructedEvent args)
    {
        // right now you don't even need to unlock the emitter to deconstruct it. that's almost certainly a bug but even without it probably still needs an alert
        AlertRadio(ent, ent.Comp.LocDeconstructed);
    }

    [SubscribeLocalEvent]
    private void OnLockToggled(Entity<NotifyOnNonFunctionalComponent> ent, ref LockToggledEvent args)
    {
        if (args.Locked)
            return;

        AlertRadio(ent, ent.Comp.LocUnlocked);
    }

    protected virtual void AlertRadio(Entity<NotifyOnNonFunctionalComponent> ent, string locString)
    {

    }
}
