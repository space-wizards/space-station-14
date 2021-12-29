using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Inventory
{
    [NetworkedComponent()]
    public abstract class SharedInventoryComponent : Component
    {
        [Dependency] protected readonly IReflectionManager ReflectionManager = default!;
        [Dependency] protected readonly IDynamicTypeFactory DynamicTypeFactory = default!;

        public sealed override string Name => "Inventory";

        [ViewVariables]
        protected Inventory InventoryInstance { get; private set; } = default!;

        [ViewVariables]
        [DataField("Template")]
        private string _templateName = "HumanInventory"; //stored for serialization purposes

        protected override void Initialize()
        {
            base.Initialize();

            CreateInventory();
        }

        private void CreateInventory()
        {
            var type = ReflectionManager.LooseGetType(_templateName);
            DebugTools.Assert(type != null);
            InventoryInstance = DynamicTypeFactory.CreateInstance<Inventory>(type!);
        }

        public abstract bool TryGetSlot(Slots slot, [NotNullWhen(true)] out EntityUid? item);

        /// <returns>true if the item is equipped to an equip slot (NOT inside an equipped container
        /// like inside a backpack)</returns>
        public abstract bool IsEquipped(EntityUid item);

        [Serializable, NetSerializable]
        protected class InventoryComponentState : ComponentState
        {
            public List<KeyValuePair<Slots, EntityUid>> Entities { get; }
            public KeyValuePair<Slots, (EntityUid entity, bool fits)>? HoverEntity { get; }

            public InventoryComponentState(List<KeyValuePair<Slots, EntityUid>> entities, KeyValuePair<Slots, (EntityUid entity, bool fits)>? hoverEntity = null)
            {
                Entities = entities;
                HoverEntity = hoverEntity;
            }
        }

        [Serializable, NetSerializable]
#pragma warning disable 618
        public class ClientInventoryMessage : ComponentMessage
#pragma warning restore 618
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
#pragma warning disable 618
        public class OpenSlotStorageUIMessage : ComponentMessage
#pragma warning restore 618
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
