using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Lock;
using Content.Shared.Radio.Components;

namespace Content.Shared.Radio.EntitySystems;

public abstract partial class SharedNotifyOnNonFunctionalSystem : EntitySystem
{

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<NotifyOnNonFunctionalComponent> ent, ref DestructionEventArgs args)
    {
        AlertRadio(ent, ent.Comp.LocDestroyed);
    }

    [SubscribeLocalEvent]
    private void OnDeconstructed(Entity<NotifyOnNonFunctionalComponent> ent, ref MachineDeconstructedEvent args)
    {
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
