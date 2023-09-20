using Content.Server.UserInterface;
using Content.Shared.Broke;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineSystem
{
    private void OnActivatableUIOpenAttempt(EntityUid uid, BrokeComponent component,
        ActivatableUIOpenAttemptEvent args)
    {
        if (component.IsBroken)
            args.Cancel();
    }

    private void OnBoundUIOpened(EntityUid uid, VendingMachineInventoryComponent component,
        BoundUIOpenedEvent args)
    {
        UpdateVendingMachineInterfaceState(uid, component);
    }

    private void UpdateVendingMachineInterfaceState(EntityUid uid,
        VendingMachineInventoryComponent component)
    {
        var state = new VendingMachineInterfaceState(_machineSystem.GetAllInventory(uid, component));

        _userInterfaceSystem.TrySetUiState(uid, VendingMachineUiKey.Key, state);
    }
}
