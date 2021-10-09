using Content.Server.VendingMachines.Components;
using Content.Shared.DragDrop;
using Content.Shared.VendingMachines;
using Robust.Shared.GameObjects;

namespace Content.Server.VendingMachines
{
    public class VendingMachineSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedVendingMachineRestockComponent, DragDropRequestEvent>(OnDropRestockCompOnVendingMachine);         
        }

        private void OnDropRestockCompOnVendingMachine(EntityUid eUI, SharedVendingMachineRestockComponent restockComp, DragDropRequestEvent args)
        {
            if (EntityManager.TryGetComponent<VendingMachineComponent>(args.Target, out var vendingMachineComp))
            {
                vendingMachineComp.RestockInventory();
            }
        }
    }
}
