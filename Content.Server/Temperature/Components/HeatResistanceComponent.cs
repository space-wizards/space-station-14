using Content.Server.Clothing.Components;
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
            if (EntitySystem.Get<InventorySystem>().TryGetSlotEntity(Owner, "gloves", out var slotEntity) &&
                IoCManager.Resolve<IEntityManager>().TryGetComponent<ClothingComponent>(slotEntity, out var gloves))
            {
                return gloves.HeatResistance;
            }
            return int.MinValue;
        }
    }
}
