using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using SS14.Client.Graphics;
using SS14.Shared.GameObjects;
using SS14.Shared.ViewVariables;
using System;

namespace Content.Client.GameObjects.Components.Clothing
{
    public class ClothingComponent : ItemComponent
    {
        public override string Name => "Clothing";
        public override uint? NetID => ContentNetIDs.CLOTHING;
        public override Type StateType => typeof(ClothingComponentState);

        [ViewVariables(VVAccess.ReadWrite)]
        public string ClothingEquippedPrefix { get; set; }

        public (RSI rsi, RSI.StateId stateId)? GetEquippedStateInfo(EquipmentSlotDefines.SlotFlags slot)
        {
            if (RsiPath == null)
            {
                return null;
            }

            var rsi = GetRSI();
            var prefix = ClothingEquippedPrefix ?? EquippedPrefix;
            var stateId = prefix != null ? $"{prefix}-equipped-{slot}" : $"equipped-{slot}";
            if (rsi.TryGetState(stateId, out _))
            {
                return (rsi, stateId);
            }

            return null;
        }

        public override void HandleComponentState(ComponentState state)
        {
            var clothingComponentState = (ClothingComponentState)state;
            ClothingEquippedPrefix = clothingComponentState.ClothingEquippedPrefix;
            EquippedPrefix = clothingComponentState.EquippedPrefix;
        }
    }
}
