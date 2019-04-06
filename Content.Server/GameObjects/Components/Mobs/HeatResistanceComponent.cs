using System;
using SS14.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.GameObjects
{
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
