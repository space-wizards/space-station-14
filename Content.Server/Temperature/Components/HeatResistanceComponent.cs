using Content.Server.Clothing.Components;
using Content.Server.Inventory.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Server.Temperature.Components
{
    [RegisterComponent]
    public class HeatResistanceComponent : Component
    {
        public override string Name => "HeatResistance";

        public int GetHeatResistance()
        {
            if (Owner.GetComponent<InventoryComponent>().TryGetSlotItem(EquipmentSlotDefines.Slots.GLOVES, out ClothingComponent? gloves))
            {
                return gloves?.HeatResistance ?? int.MinValue;
            }
            return int.MinValue;
        }
    }
}
