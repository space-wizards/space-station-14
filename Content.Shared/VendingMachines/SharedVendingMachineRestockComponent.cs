#nullable enable
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;

namespace Content.Shared.VendingMachines
{
    /// <summary>
    /// Entity which can dropped on a vending machine to restock it.
    /// </summary>
    [RegisterComponent]
    public class SharedVendingMachineRestockComponent : Component, IDraggable
    {
        public override string Name => "VendingMachineRestock";

        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return true;
        }
    }
}
