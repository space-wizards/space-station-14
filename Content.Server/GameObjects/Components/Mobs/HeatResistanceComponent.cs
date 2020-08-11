using CannyFastMath;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class HeatResistanceComponent : Component
    {
        public override string Name => "HeatResistance";

        public int GetHeatResistance()
        {
            if (Owner.GetComponent<InventoryComponent>().TryGetSlotItem(EquipmentSlotDefines.Slots.GLOVES, itemComponent: out ClothingComponent gloves)
             | Owner.TryGetComponent(out SpeciesComponent speciesComponent))
            {
                return Math.Max(gloves?.HeatResistance ?? int.MinValue, speciesComponent?.HeatResistance ?? int.MinValue);
            }
            return int.MinValue;
        }
    }
}
