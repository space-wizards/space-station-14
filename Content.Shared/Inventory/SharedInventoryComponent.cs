using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Movement.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
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
    [Friend(typeof(SharedInventorySystem))]
    public abstract class SharedInventoryComponent : Component
    {
        [Dependency] protected readonly IReflectionManager ReflectionManager = default!;
        [Dependency] protected readonly IDynamicTypeFactory DynamicTypeFactory = default!;

        public sealed override string Name => "Inventory";

        [ViewVariables] public readonly Dictionary<Slots, string> SlotContainers = new();

        [ViewVariables]
        public Inventory InventoryInstance = default!;

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

        protected override void OnRemove()
        {
            var slots = _slotContainers.Keys.ToList();

            foreach (var slot in slots)
            {
                if (TryGetSlotItem(slot, out ItemComponent? item))
                {
                    item.Owner.Delete();
                }

                RemoveSlot(slot);
            }

            base.OnRemove();
        }

        public IEnumerable<IEntity> GetAllHeldItems()
        {
            foreach (var (_, containerId) in _slotContainers)
            {
                if(_containerManagerComponent != null && _containerManagerComponent.TryGetContainer(containerId, out var container))
                {
                    foreach (var entity in container.ContainedEntities)
                    {
                        yield return entity;
                    }
                }
            }
        }

        /// <returns>true if the item is equipped to an equip slot (NOT inside an equipped container
        /// like inside a backpack)</returns>
        public abstract bool IsEquipped(IEntity item);

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
