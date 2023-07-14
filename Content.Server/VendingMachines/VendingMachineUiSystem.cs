using Content.Server.UserInterface;
using Content.Shared.Broke;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Server.GameObjects;

namespace Content.Server.VendingMachines;

public sealed class VendingMachineUiSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly VendingMachineSystem _machineSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrokeComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
        SubscribeLocalEvent<VendingMachineInventoryComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnActivatableUIOpenAttempt(EntityUid uid, BrokeComponent component,
        ActivatableUIOpenAttemptEvent args)
    {
        if (component.Broken)
            args.Cancel();
    }

    private void OnBoundUIOpened(EntityUid uid, VendingMachineInventoryComponent component,
        BoundUIOpenedEvent args)
    {
        UpdateVendingMachineInterfaceState(uid, component);
    }

    public void UpdateVendingMachineInterfaceState(EntityUid uid,
        VendingMachineInventoryComponent component)
    {
        var state = new VendingMachineInterfaceState(_machineSystem.GetAllInventory(uid, component));

        _userInterfaceSystem.TrySetUiState(uid, VendingMachineUiKey.Key, state);
    }
}
