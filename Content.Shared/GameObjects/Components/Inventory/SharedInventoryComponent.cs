using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.GameObjects
{
    public abstract class SharedInventoryComponent : Component
    {
        public sealed override string Name => "Inventory";
        public sealed override uint? NetID => ContentNetIDs.STORAGE;
        public sealed override Type StateType => typeof(InventoryComponentState);

        [Serializable, NetSerializable]
        protected class InventoryComponentState : ComponentState
        {
            public List<KeyValuePair<Slots, EntityUid>> Entities { get; }

            public InventoryComponentState(List<KeyValuePair<Slots, EntityUid>> entities) : base(ContentNetIDs.STORAGE)
            {
                Entities = entities;
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
