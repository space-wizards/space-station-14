using SS14.Shared.GameObjects.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects
{
    public class ClothingComponent : ItemComponent
    {
        public override string Name => "Clothing";
        public SlotFlags SlotFlags = SlotFlags.PREVENTEQUIP; //Different from None, NONE allows equips if no slot flags are required
        private List<string> slotstrings = new List<string>(); //serialization

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);
            
            serializer.DataField(ref slotstrings, "Slots", new List<string>(0));

            foreach(var slotflagsloaded in slotstrings)
            {
                SlotFlags |= (SlotFlags)Enum.Parse(typeof(SlotFlags), slotflagsloaded.ToUpper());
            }   
        }
    }
}
