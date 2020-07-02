using System;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.GameObjects;
using CannyFastMath;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Content.Server.GameObjects
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
