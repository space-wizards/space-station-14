using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Wires;
using Robust.Shared.Audio;

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
            if (_net.IsServer)
            {
                Popup.PopupCursor(Loc.GetString("vending-machine-restock-needs-panel-open",
                        ("this", uid),
                        ("user", user),
                        ("target", target)),
                    user);
            }

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
            if (_net.IsServer)
            {
                Popup.PopupCursor(Loc.GetString("vending-machine-restock-invalid-inventory", ("this", uid), ("user", user),
                        ("target", target)), user);
            }

            return false;
        }

        return true;
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

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, (float)component.RestockDelay.TotalSeconds, new RestockDoAfterEvent(), target,
            target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        if (_net.IsServer)
        {
            Popup.PopupEntity(Loc.GetString("vending-machine-restock-start", ("this", uid), ("user", args.User),
                    ("target", target)),
                args.User,
                PopupType.Medium);
        }

        Audio.PlayPredicted(component.SoundRestockStart, uid, args.User);
    }
}
