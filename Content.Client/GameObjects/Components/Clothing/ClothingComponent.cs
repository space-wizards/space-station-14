using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
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

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
                return;

            var clothingComponentState = (ClothingComponentState)curState;
            ClothingEquippedPrefix = clothingComponentState.ClothingEquippedPrefix;
            EquippedPrefix = clothingComponentState.EquippedPrefix;
        }
    }
}
