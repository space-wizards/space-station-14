using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.GameObjects;
using Math = CannyFastMath.Math;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    public class HeatResistanceComponent : Component
    {
        public override string Name => "HeatResistance";

        public int GetHeatResistance()
        {
            if (Owner.GetComponent<InventoryComponent>().TryGetSlotItem(EquipmentSlotDefines.Slots.GLOVES, itemComponent: out ClothingComponent gloves))
            {
                return gloves?.HeatResistance ?? int.MinValue;
            }
            return int.MinValue;
        }
    }
}
