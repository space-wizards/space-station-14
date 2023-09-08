using Content.Shared.Emag.Systems;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineSystem
{
    private void OnEmagged(EntityUid uid, VendingMachineInventoryComponent component, ref GotEmaggedEvent args)
    {
        var emaggedItems = component.Inventories.GetValueOrDefault(VendingMachinesInventoryTypeNames.Emagged);

        if (emaggedItems == null)
        {
            args.Handled = false;

            return;
        }

        // only emag if there are emag-only items
        args.Handled = emaggedItems.Count > 0;
    }
}
