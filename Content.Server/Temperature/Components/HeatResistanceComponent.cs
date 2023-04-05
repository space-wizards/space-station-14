using Content.Server.Clothing.Components;
using Content.Shared.Inventory;

namespace Content.Server.Temperature.Components
{
    [RegisterComponent]
    public sealed class HeatResistanceComponent : Component
    {
        public int GetHeatResistance()
        {
            // TODO: When making into system: Any animal that touches bulb that has no
            // InventoryComponent but still would have default heat resistance in the future (maybe)
            if (EntitySystem.Get<InventorySystem>().TryGetSlotEntity(Owner, "gloves", out var slotEntity) &&
                IoCManager.Resolve<IEntityManager>().TryGetComponent<GloveHeatResistanceComponent>(slotEntity, out var gloves))
            {
                return gloves.HeatResistance;
            }
            return int.MinValue;
        }
    }
}
