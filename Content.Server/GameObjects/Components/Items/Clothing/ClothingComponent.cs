using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects
{
    public class ClothingComponent : ItemComponent
    {
        public override string Name => "Clothing";
        public override uint? NetID => ContentNetIDs.CLOTHING;
        public override Type StateType => typeof(ClothingComponentState);

        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required

        public string ClothingEquippedPrefix { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO: Writing.
            serializer.DataReadFunction("Slots", new List<string>(0), list =>
            {
                foreach (var slotflagsloaded in list)
                {
                    SlotFlags |= (SlotFlags)Enum.Parse(typeof(SlotFlags), slotflagsloaded.ToUpper());
                }
            });
        }

        public override ComponentState GetComponentState()
        {
            return new ClothingComponentState(ClothingEquippedPrefix);
        }
    }
}
