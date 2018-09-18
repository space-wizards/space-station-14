using Content.Shared.GameObjects.Components.Inventory;
using SS14.Client.Graphics;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;

namespace Content.Client.GameObjects.Components.Clothing
{
    public class ClothingComponent : Component
    {
        public override string Name => "Clothing";

        public (ResourcePath rsiPath, RSI.StateId stateId) GetEquippedStateInfo(EquipmentSlotDefines.SlotFlags slot)
        {

        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            
        }
    }
}
