using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.GameObjects.Components.Inventory
{
    public abstract class SharedInventoryComponent : Component
    {
        // ReSharper disable UnassignedReadonlyField
        [Dependency] protected readonly IReflectionManager ReflectionManager;
        [Dependency] protected readonly IDynamicTypeFactory DynamicTypeFactory;
        // ReSharper restore UnassignedReadonlyField

        public sealed override string Name => "Inventory";
        public sealed override uint? NetID => ContentNetIDs.STORAGE;

        [ViewVariables]
        protected Inventory InventoryInstance { get; private set; }

        [ViewVariables]
        private string _templateName = "HumanInventory"; //stored for serialization purposes

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _templateName, "Template", "HumanInventory");
        }

        public override void Initialize()
        {
            base.Initialize();

            CreateInventory();
        }

        private void CreateInventory()
        {
            var type = ReflectionManager.LooseGetType(_templateName);
            DebugTools.Assert(type != null);
            InventoryInstance = DynamicTypeFactory.CreateInstance<Inventory>(type);
        }

        /// <returns>true if the item is equipped to an equip slot (NOT inside an equipped container
        /// like inside a backpack)</returns>
        public abstract bool IsEquipped(IEntity item);

        [Serializable, NetSerializable]
        protected class InventoryComponentState : ComponentState
        {
            public List<KeyValuePair<Slots, EntityUid>> Entities { get; }
            public KeyValuePair<Slots, (EntityUid entity, bool fits)>? HoverEntity { get; }

            public InventoryComponentState(List<KeyValuePair<Slots, EntityUid>> entities, KeyValuePair<Slots, (EntityUid entity, bool fits)>? hoverEntity = null) : base(ContentNetIDs.STORAGE)
            {
                Entities = entities;
                HoverEntity = hoverEntity;
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
                Use = 1,
                Hover = 2
            }
        }

        /// <summary>
        /// Component message for opening the Storage UI of item in Slot
        /// </summary>
        [Serializable, NetSerializable]
        public class OpenSlotStorageUIMessage : ComponentMessage
        {
            public Slots Slot;

            public OpenSlotStorageUIMessage(Slots slot)
            {
                Directed = true;
                Slot = slot;
            }
        }
    }
}
