using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.GameObjects
{
    public abstract class SharedInventoryComponent : Component
    {
        public sealed override string Name => "Inventory";

        public override uint? NetID => ContentNetIDs.STORAGE;


        [Serializable, NetSerializable]
        public class ServerInventoryMessage : ComponentMessage
        {
            public Slots Inventoryslot;
            public EntityUid EntityUid;
            public ServerInventoryUpdate Updatetype;

            public ServerInventoryMessage()
            {
                Directed = true;
            }

            public enum ServerInventoryUpdate
            {
                Removal = 0,
                Addition = 1
            }
        }

        [Serializable, NetSerializable]
        public class ClientInventoryMessage : ComponentMessage
        {
            public Slots Inventoryslot;
            public ClientInventoryUpdate Updatetype;

            public ClientInventoryMessage(Slots inventoryslot, ClientInventoryUpdate updatetype)
            {
                Directed = true;
                Inventoryslot = inventoryslot;
                Updatetype = updatetype;
            }

            public enum ClientInventoryUpdate
            {
                Equip = 0,
                Unequip = 1
            }
        }
    }
}
