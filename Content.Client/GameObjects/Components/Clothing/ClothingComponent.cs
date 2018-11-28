using Content.Shared.GameObjects.Components.Inventory;
using SS14.Client.Graphics;

namespace Content.Client.GameObjects.Components.Clothing
{
    public class ClothingComponent : ItemComponent
    {
        public override string Name => "Clothing";

        public (RSI rsi, RSI.StateId stateId)? GetEquippedStateInfo(EquipmentSlotDefines.SlotFlags slot)
        {
            if (RsiPath == null)
            {
                return null;
            }

            var rsi = GetRSI();
            var stateId = EquippedPrefix != null ? $"{EquippedPrefix}-equipped-{slot}" : $"equipped-{slot}";
            if (rsi.TryGetState(stateId, out _))
            {
                return (rsi, stateId);
            }

            return null;
        }
    }
}
