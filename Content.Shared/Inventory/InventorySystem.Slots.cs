using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

public partial class InventorySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public bool TryGetSlotContainer(EntityUid uid, string slot, [NotNullWhen(true)] out ContainerSlot? containerSlot, [NotNullWhen(true)] out SlotDefinition? slotDefinition,
        InventoryComponent? inventory = null, ContainerManagerComponent? containerComp = null)
    {
        containerSlot = null;
        slotDefinition = null;
        if (!Resolve(uid, ref containerComp, ref inventory, false))
            return false;

        if (!TryGetSlot(uid, slot, out slotDefinition, out var isInvSlot, inventory: inventory))
            return false;

        //this overrides the containerComp var with the containerComp of the inventorySlotEntity. its janky, but it works
        if (isInvSlot != null && !TryComp(isInvSlot, out containerComp)) return false;

        if (!containerComp.TryGetContainer(slotDefinition.Name, out var container))
        {
            containerSlot = containerComp.MakeContainer<ContainerSlot>(slotDefinition.Name);
            containerSlot.OccludesLight = false;
            return true;
        }

        if (container is not ContainerSlot containerSlotChecked) return false;

        containerSlot = containerSlotChecked;
        return true;
    }

    public bool HasSlot(EntityUid uid, string slot, InventoryComponent? component = null) =>
        TryGetSlot(uid, slot, out _, out _, component);

    public bool TryGetSlot(EntityUid uid, string slot, [NotNullWhen(true)] out SlotDefinition? slotDefinition, out EntityUid? isFromInvSlotEntity, InventoryComponent? inventory = null)
    {
        slotDefinition = null;
        isFromInvSlotEntity = null;
        if (!Resolve(uid, ref inventory, false)) return false;

        static bool TryGetSlotDefFromArray(SlotDefinition[] slotDefinitions, string slotId, [NotNullWhen(true)] out SlotDefinition? slotDefParam)
        {
            foreach (var slotDef in slotDefinitions)
            {
                if (!slotDef.Name.Equals(slotId)) continue;
                slotDefParam = slotDef;
                return true;
            }

            slotDefParam = null;
            return false;
        }

        if (_prototypeManager.TryIndex<InventoryTemplatePrototype>(inventory.TemplateId, out var templatePrototype) &&
            TryGetSlotDefFromArray(templatePrototype.Slots, slot, out slotDefinition)) return true;

        foreach (var slotSlot in inventory.InventorySlotSlots)
        {
            if (!TryGetSlotEntity(uid, slotSlot, out var slotEntity, inventory)) continue;

            if (TryComp<InventorySlotComponent>(slotEntity, out var invSlotComp) &&
                TryGetSlotDefFromArray(invSlotComp.Slots, slot, out slotDefinition))
            {
                isFromInvSlotEntity = slotEntity;
                return true;
            }
        }

        return false;
    }

    public bool TryGetContainerSlotEnumerator(EntityUid uid, out ContainerSlotEnumerator containerSlotEnumerator, SlotFlags flags = SlotFlags.All, InventoryComponent? component = null)
    {
        containerSlotEnumerator = default;
        if (!Resolve(uid, ref component, false))
            return false;

        containerSlotEnumerator = new ContainerSlotEnumerator(uid, component, _prototypeManager, this);
        return true;
    }

    public bool TryGetSlotEnumerator(EntityUid uid, out SlotDefinitionEnumerator enumerator, SlotFlags flags = SlotFlags.All, InventoryComponent? inventoryComponent = null)
    {
        enumerator = default;
        if (!Resolve(uid, ref inventoryComponent))
            return false;
        enumerator = new SlotDefinitionEnumerator(uid, inventoryComponent, _prototypeManager, this, flags);
        return true;
    }

    public struct SlotDefinitionEnumerator
    {
        private readonly InventorySystem _inventorySystem;
        private readonly EntityUid _uid;
        private readonly List<SlotDefinition[]> _slots = new();
        private readonly SlotFlags _flags;
        private int _nextIdx = 0;
        private int _currentArrayIndex = 0;

        public SlotDefinitionEnumerator(EntityUid uid, InventoryComponent inventoryComponent, IPrototypeManager prototypeManager, InventorySystem inventorySystem, SlotFlags flags = SlotFlags.All)
        {
            _uid = uid;
            _inventorySystem = inventorySystem;
            _flags = flags;

            if (prototypeManager.TryIndex<InventoryTemplatePrototype>(inventoryComponent.TemplateId, out var prototype))
                _slots.Add(prototype.Slots);

            foreach (var slotSlot in inventoryComponent.InventorySlotSlots)
            {
                if(!inventorySystem.TryGetSlotEntity(uid, slotSlot, out var slotEntity, inventoryComponent)) continue;

                if (inventorySystem.TryComp<InventorySlotComponent>(slotEntity.Value, out var slotComponent))
                {
                    _slots.Add(slotComponent.Slots);
                }
            }
        }

        public bool MoveNext([NotNullWhen(true)] out SlotDefinition? slotDef)
        {
            slotDef = null;

            while (_currentArrayIndex < _slots.Count)
            {
                while (_nextIdx < _slots[_currentArrayIndex].Length)
                {
                    var slot = _slots[_currentArrayIndex][_nextIdx++];

                    if ((slot.SlotFlags & _flags) == 0)
                        continue;

                    if (_inventorySystem.TryGetSlot(_uid, slot.Name, out slotDef, out _))
                        return true;
                }

                _nextIdx = 0;
                _currentArrayIndex++;
            }

            return false;
        }
    }

    public struct ContainerSlotEnumerator
    {
        private SlotDefinitionEnumerator _enumerator;
        private readonly InventorySystem _inventorySystem;
        private readonly EntityUid _uid;

        public ContainerSlotEnumerator(EntityUid uid, InventoryComponent inventoryComponent,
            IPrototypeManager prototypeManager, InventorySystem inventorySystem, SlotFlags flags = SlotFlags.All)
        {
            _enumerator =
                new SlotDefinitionEnumerator(uid, inventoryComponent, prototypeManager, inventorySystem, flags);
            _inventorySystem = inventorySystem;
            _uid = uid;
        }

        public bool MoveNext([NotNullWhen(true)] out ContainerSlot? container)
        {
            container = null;
            while (_enumerator.MoveNext(out var slotDef))
            {
                if (_inventorySystem.TryGetSlotContainer(_uid, slotDef.Name, out container, out _))
                    return true;
            }

            return false;
        }
    }
}

