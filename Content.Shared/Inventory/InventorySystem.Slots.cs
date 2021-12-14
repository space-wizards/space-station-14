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
        if (!Resolve(uid, ref inventory, ref containerComp))
            return false;

        if (!TryGetSlot(uid, slot, out slotDefinition, inventory))
            return false;

        if (!TryGetSlotContainerString(uid, slot, out var containerString, slotDefinition, inventory))
            return false;

        if (!containerComp.TryGetContainer(containerString, out var container))
        {
            containerSlot = containerComp.MakeContainer<ContainerSlot>(containerString);
            return true;
        }

        if (container is not ContainerSlot containerSlotChecked) return false;

        containerSlot = containerSlotChecked;
        return true;
    }

    public bool TryGetSlot(EntityUid uid, string slot, [NotNullWhen(true)] out SlotDefinition? slotDefinition, InventoryComponent? inventory = null)
    {
        slotDefinition = null;
        if (!Resolve(uid, ref inventory))
            return false;

        if (!_prototypeManager.TryIndex<InventoryTemplatePrototype>(inventory.TemplateId, out var templatePrototype))
            return false;

        slotDefinition = templatePrototype.Slots.FirstOrDefault(x => x.Name == slot);
        return slotDefinition != default;
    }

    public bool SlotExists(EntityUid uid, string slot, InventoryComponent? inventory = null) =>
        TryGetSlot(uid, slot, out _, inventory);

    public bool TryGetSlotContainerString(EntityUid uid, string slot, [NotNullWhen(true)] out string? containerString, SlotDefinition? slotDefinition = null, InventoryComponent? inventory = null)
    {
        containerString = string.Empty;

        if (!Resolve(uid, ref inventory))
            return false;

        if (slotDefinition == null && !TryGetSlot(uid, slot, out slotDefinition, inventory))
            return false;

        containerString = $"{inventory.Name}_{slotDefinition.Name}";
        return true;
    }

    public bool TryGetContainerSlotEnumerator(EntityUid uid, out ContainerSlotEnumerator containerSlotEnumerator, InventoryComponent? component = null)
    {
        containerSlotEnumerator = default;
        if (!Resolve(uid, ref component))
            return false;

        containerSlotEnumerator = new ContainerSlotEnumerator(uid, component.TemplateId, _prototypeManager, this);
        return true;
    }

    public bool TryGetSlots(EntityUid uid, [NotNullWhen(true)] out SlotDefinition[]? slotDefinitions, InventoryComponent? inventoryComponent = null)
    {
        slotDefinitions = null;
        if (!Resolve(uid, ref inventoryComponent))
            return false;

        if (!_prototypeManager.TryIndex<InventoryTemplatePrototype>(inventoryComponent.TemplateId, out var templatePrototype))
            return false;

        slotDefinitions = templatePrototype.Slots;
        return true;
    }

    public struct ContainerSlotEnumerator
    {
        private readonly InventorySystem _inventorySystem;
        private readonly EntityUid _uid;
        private readonly SlotDefinition[] _slots;
        private int _nextIdx = -1;

        public ContainerSlotEnumerator(EntityUid uid, string prototypeId, IPrototypeManager prototypeManager, InventorySystem inventorySystem)
        {
            _uid = uid;
            _inventorySystem = inventorySystem;
            if (prototypeManager.TryIndex<InventoryTemplatePrototype>(prototypeId, out var prototype))
            {
                _slots = prototype.Slots;
                if(_slots.Length > 0)
                    _nextIdx = 0;
            }
            else
            {
                _slots = Array.Empty<SlotDefinition>();
            }
        }

        public bool MoveNext([NotNullWhen(true)] out ContainerSlot? container)
        {
            container = null;
            if (_nextIdx == -1 || _nextIdx >= _slots.Length) return false;

            while (_nextIdx < _slots.Length && !_inventorySystem.TryGetSlotContainer(_uid, _slots[_nextIdx++].Name, out container, out _)) { }

            return container != null;
        }
    }
}
