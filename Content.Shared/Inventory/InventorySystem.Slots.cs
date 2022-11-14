using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

public partial class InventorySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private void InitializeSlots()
    {
        SubscribeLocalEvent<InventoryComponent, ComponentInit>(OnInit);
    }

    protected virtual void OnInit(EntityUid uid, InventoryComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.TemplateId, out InventoryTemplatePrototype? invTemplate))
            return;

        foreach (var slot in invTemplate.Slots)
        {
            _containerSystem.EnsureContainer<ContainerSlot>(uid, slot.Name).OccludesLight = false;
        }
    }

    public bool TryGetSlotContainer(EntityUid uid, string slot, [NotNullWhen(true)] out ContainerSlot? containerSlot, [NotNullWhen(true)] out SlotDefinition? slotDefinition,
        InventoryComponent? inventory = null, ContainerManagerComponent? containerComp = null)
    {
        containerSlot = null;
        slotDefinition = null;
        if (!Resolve(uid, ref inventory, ref containerComp, false))
            return false;

        if (!TryGetSlot(uid, slot, out slotDefinition, inventory: inventory))
            return false;

        if (!containerComp.TryGetContainer(slotDefinition.Name, out var container))
        {
            if (inventory.LifeStage >= ComponentLifeStage.Initialized)
                Logger.Error($"Missing inventory container {slot} on entity {ToPrettyString(uid)}");
            return false;
        }

        if (container is not ContainerSlot containerSlotChecked) return false;

        containerSlot = containerSlotChecked;
        return true;
    }

    public bool HasSlot(EntityUid uid, string slot, InventoryComponent? component = null) =>
        TryGetSlot(uid, slot, out _, component);

    public bool TryGetSlot(EntityUid uid, string slot, [NotNullWhen(true)] out SlotDefinition? slotDefinition, InventoryComponent? inventory = null)
    {
        slotDefinition = null;
        if (!Resolve(uid, ref inventory, false))
            return false;

        if (!_prototypeManager.TryIndex<InventoryTemplatePrototype>(inventory.TemplateId, out var templatePrototype))
            return false;

        foreach (var slotDef in templatePrototype.Slots)
        {
            if (!slotDef.Name.Equals(slot)) continue;
            slotDefinition = slotDef;
            return true;
        }

        return false;
    }

    public bool TryGetContainerSlotEnumerator(EntityUid uid, out ContainerSlotEnumerator containerSlotEnumerator, InventoryComponent? component = null)
    {
        containerSlotEnumerator = default;
        if (!Resolve(uid, ref component, false))
            return false;

        containerSlotEnumerator = new ContainerSlotEnumerator(uid, component.TemplateId, _prototypeManager, this);
        return true;
    }

    public bool TryGetSlots(EntityUid uid, [NotNullWhen(true)] out SlotDefinition[]? slotDefinitions, InventoryComponent? inventoryComponent = null)
    {
        slotDefinitions = null;
        if (!Resolve(uid, ref inventoryComponent, false))
            return false;

        if (!_prototypeManager.TryIndex<InventoryTemplatePrototype>(inventoryComponent.TemplateId, out var templatePrototype))
            return false;

        slotDefinitions = templatePrototype.Slots;
        return true;
    }

    public SlotDefinition[] GetSlots(EntityUid uid, InventoryComponent? inventoryComponent = null)
    {
        if (!Resolve(uid, ref inventoryComponent)) throw new InvalidOperationException();
        return _prototypeManager.Index<InventoryTemplatePrototype>(inventoryComponent.TemplateId).Slots;
    }

    public struct ContainerSlotEnumerator
    {
        private readonly InventorySystem _inventorySystem;
        private readonly EntityUid _uid;
        private readonly SlotDefinition[] _slots;
        private readonly SlotFlags _flags;
        private int _nextIdx = 0;

        public ContainerSlotEnumerator(EntityUid uid, string prototypeId, IPrototypeManager prototypeManager, InventorySystem inventorySystem, SlotFlags flags = SlotFlags.All)
        {
            _uid = uid;
            _inventorySystem = inventorySystem;
            _flags = flags;

            if (prototypeManager.TryIndex<InventoryTemplatePrototype>(prototypeId, out var prototype))
                _slots = prototype.Slots;
            else
                _slots = Array.Empty<SlotDefinition>();
        }

        public bool MoveNext([NotNullWhen(true)] out ContainerSlot? container)
        {
            container = null;

            while (_nextIdx < _slots.Length)
            {
                var slot = _slots[_nextIdx];
                _nextIdx++;

                if ((slot.SlotFlags & _flags) == 0)
                    continue;

                if (_inventorySystem.TryGetSlotContainer(_uid, slot.Name, out container, out _))
                    return true;
            }

            return false;
        }
    }
}
