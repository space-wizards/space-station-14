using Content.Server.Clothing.Components;
using Content.Server.Inventory.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Temperature.Components
{
    [RegisterComponent]
    public class HeatResistanceComponent : Component
    {
        public override string Name => "HeatResistance";

        public int GetHeatResistance()
        {
            // TODO: When making into system: Any animal that touches bulb that has no
            // InventoryComponent but still would have default heat resistance in the future (maybe)
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<InventoryComponent?>(Owner, out var inventoryComp))
            {
                // Magical number just copied from below
                return int.MinValue;
            }

            if (inventoryComp.TryGetSlotItem(EquipmentSlotDefines.Slots.GLOVES, out ClothingComponent? gloves))
            {
                return gloves?.HeatResistance ?? int.MinValue;
            }
            return int.MinValue;
        }
    }
}
