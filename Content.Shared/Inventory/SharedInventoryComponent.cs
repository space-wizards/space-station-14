using System;
using System.Collections.Generic;
using Content.Shared.Movement.Components;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Inventory
{
    public abstract class SharedInventoryComponent : Component, IMoveSpeedModifier
    {
        [Dependency] protected readonly IReflectionManager ReflectionManager = default!;
        [Dependency] protected readonly IDynamicTypeFactory DynamicTypeFactory = default!;

        public sealed override string Name => "Inventory";
        public sealed override uint? NetID => ContentNetIDs.STORAGE;

        [ViewVariables]
        protected Inventory InventoryInstance { get; private set; } = default!;

        [ViewVariables]
        [DataField("Template")]
        private string _templateName = "HumanInventory"; //stored for serialization purposes

        public override void Initialize()
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

        public abstract float WalkSpeedModifier { get; }
        public abstract float SprintSpeedModifier { get; }
    }
}
