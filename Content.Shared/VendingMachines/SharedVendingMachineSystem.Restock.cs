using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Wires;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem
{
    public bool TryAccessMachine(EntityUid uid,
        VendingMachineRestockComponent restock,
        VendingMachineComponent machineComponent,
        EntityUid user,
        EntityUid target)
    {
        if (!TryComp<WiresPanelComponent>(target, out var panel) || !panel.Open)
        {
            Popup.PopupPredictedCursor(Loc.GetString("vending-machine-restock-needs-panel-open",
                    ("this", uid),
                    ("user", user),
                    ("target", target)),
                user);

            return false;
        }

        return true;
    }

    public bool TryMatchPackageToMachine(EntityUid uid,
        VendingMachineRestockComponent component,
        VendingMachineComponent machineComponent,
        EntityUid user,
        EntityUid target)
    {
        if (!component.CanRestock.Contains(machineComponent.PackPrototypeId))
        {
            Popup.PopupPredictedCursor(Loc.GetString("vending-machine-restock-invalid-inventory", ("this", uid), ("user", user),
                ("target", target)), user);

            return false;
        }

        return true;
    }

    public void TryRestockInventory(EntityUid uid, VendingMachineComponent? vendComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return;

        RestockInventoryFromPrototype(uid, vendComponent);

        Dirty(uid, vendComponent);
        TryUpdateVisualState((uid, vendComponent));
    }

    private void OnAfterInteract(EntityUid uid, VendingMachineRestockComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach || args.Handled)
            return;

        if (!TryComp<VendingMachineComponent>(args.Target, out var machineComponent))
            return;

        if (!TryMatchPackageToMachine(uid, component, machineComponent, args.User, target))
            return;

        if (!TryAccessMachine(uid, component, machineComponent, args.User, target))
            return;

        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            component.RestockDelay,
            new RestockDoAfterEvent(),
            target,
            target: target,
            used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        var selfMessage = Loc.GetString("vending-machine-restock-start-self", ("target", target));
        var othersMessage = Loc.GetString("vending-machine-restock-start-others",
            ("user", Identity.Entity(args.User, EntityManager)),
            ("target", target));
        Popup.PopupPredicted(selfMessage, othersMessage, target, args.User, PopupType.Medium);


        if (!Timing.IsFirstTimePredicted)
            return;

        Audio.Stop(machineComponent.RestockStream);
        machineComponent.RestockStream = Audio.PlayPredicted(component.SoundRestockStart, target, args.User)?.Entity;
    }

    private void OnRestockDoAfter(Entity<VendingMachineComponent> ent, ref RestockDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            // Future predicted ticks can clobber the RestockStream with null while not stopping anything
            if (Timing.IsFirstTimePredicted)
                ent.Comp.RestockStream = Audio.Stop(ent.Comp.RestockStream);
            return;
        }

        if (args.Handled || args.Used == null)
            return;

        if (!TryComp<VendingMachineRestockComponent>(args.Used, out var restockComponent))
        {
            Log.Error($"{ToPrettyString(args.User)} tried to restock {ToPrettyString(ent)} with {ToPrettyString(args.Used.Value)} which did not have a VendingMachineRestockComponent.");
            return;
        }

        TryRestockInventory(ent, ent.Comp);

        var userMessage = Loc.GetString("vending-machine-restock-done-self", ("target", ent));
        var othersMessage = Loc.GetString("vending-machine-restock-done-others",
            ("user", Identity.Entity(args.User, EntityManager)),
            ("target", ent));
        Popup.PopupPredicted(userMessage, othersMessage, ent, args.User, PopupType.Medium);

        Audio.PlayPredicted(restockComponent.SoundRestockDone, ent, args.User);

        PredictedQueueDel(args.Used.Value);
    }
}
