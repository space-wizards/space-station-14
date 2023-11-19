using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Inventory;

public partial class InventorySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IViewVariablesManager _vvm = default!;

    private void InitializeSlots()
    {
        SubscribeLocalEvent<InventoryComponent, ComponentInit>(OnInit);

        _vvm.GetTypeHandler<InventoryComponent>()
            .AddHandler(HandleViewVariablesSlots, ListViewVariablesSlots);
    }

    private void ShutdownSlots()
    {
        _vvm.GetTypeHandler<InventoryComponent>()
            .RemoveHandler(HandleViewVariablesSlots, ListViewVariablesSlots);
    }

    protected virtual void OnInit(EntityUid uid, InventoryComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.TemplateId, out InventoryTemplatePrototype? invTemplate))
            return;

        component.Slots = invTemplate.Slots;
        component.Containers = new ContainerSlot[component.Slots.Length];
        for (var i = 0; i < component.Containers.Length; i++)
        {
            var slot = component.Slots[i];
            var container = _containerSystem.EnsureContainer<ContainerSlot>(uid, slot.Name);
            container.OccludesLight = false;
            component.Containers[i] = container;
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
                Log.Error($"Missing inventory container {slot} on entity {ToPrettyString(uid)}");
            return false;
        }

        if (container is not ContainerSlot containerSlotChecked)
            return false;

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

        foreach (var slotDef in inventory.Slots)
        {
            if (!slotDef.Name.Equals(slot))
                continue;
            slotDefinition = slotDef;
            return true;
        }

        return false;
    }

    public bool TryGetContainerSlotEnumerator(Entity<InventoryComponent?> entity, out InventorySlotEnumerator containerSlotEnumerator, SlotFlags flags = SlotFlags.All)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
        {
            containerSlotEnumerator = default;
            return false;
        }

        containerSlotEnumerator = new InventorySlotEnumerator(entity.Comp, flags);
        return true;
    }

    public InventorySlotEnumerator GetSlotEnumerator(Entity<InventoryComponent?> entity, SlotFlags flags = SlotFlags.All)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return InventorySlotEnumerator.Empty;

        return new InventorySlotEnumerator(entity.Comp, flags);
    }

    public bool TryGetSlots(EntityUid uid, [NotNullWhen(true)] out SlotDefinition[]? slotDefinitions)
    {
        if (!TryComp(uid, out InventoryComponent? inv))
        {
            slotDefinitions = null;
            return false;
        }

        slotDefinitions = inv.Slots;
        return true;
    }

    private ViewVariablesPath? HandleViewVariablesSlots(EntityUid uid, InventoryComponent comp, string relativePath)
    {
        return TryGetSlotEntity(uid, relativePath, out var entity, comp)
            ? ViewVariablesPath.FromObject(entity)
            : null;
    }

    private IEnumerable<string> ListViewVariablesSlots(EntityUid uid, InventoryComponent comp)
    {
        foreach (var slotDef in comp.Slots)
        {
            yield return slotDef.Name;
        }
    }

    /// <summary>
    /// Enumerator for iterating over an inventory's slot containers. Also has methods that skip empty containers.
    /// It should be safe to add or remove items while enumerating.
    /// </summary>
    public struct InventorySlotEnumerator
    {
        private readonly SlotDefinition[] _slots;
        private readonly ContainerSlot[] _containers;
        private readonly SlotFlags _flags;
        private int _nextIdx = 0;
        public static InventorySlotEnumerator Empty = new(Array.Empty<SlotDefinition>(), Array.Empty<ContainerSlot>());

        public InventorySlotEnumerator(InventoryComponent inventory,  SlotFlags flags = SlotFlags.All)
            : this(inventory.Slots, inventory.Containers, flags)
        {
        }

        public InventorySlotEnumerator(SlotDefinition[] slots, ContainerSlot[] containers,  SlotFlags flags = SlotFlags.All)
        {
            DebugTools.Assert(flags != SlotFlags.NONE);
            DebugTools.AssertEqual(slots.Length, containers.Length);
            _flags = flags;
            _slots = slots;
            _containers = containers;
        }

        public bool MoveNext([NotNullWhen(true)] out ContainerSlot? container)
        {
            while (_nextIdx < _slots.Length)
            {
                var i = _nextIdx++;
                var slot = _slots[i];

                if ((slot.SlotFlags & _flags) == 0)
                    continue;

                container = _containers[i];
                return true;
            }

            container = null;
            return false;
        }

        public bool NextItem(out EntityUid item)
        {
            while (_nextIdx < _slots.Length)
            {
                var i = _nextIdx++;
                var slot = _slots[i];

                if ((slot.SlotFlags & _flags) == 0)
                    continue;

                var container = _containers[i];
                if (container.ContainedEntity is { } uid)
                {
                    item = uid;
                    return true;
                }
            }

            item = default;
            return false;
        }

        public bool NextItem(out EntityUid item, [NotNullWhen(true)] out SlotDefinition? slot)
        {
            while (_nextIdx < _slots.Length)
            {
                var i = _nextIdx++;
                slot = _slots[i];

                if ((slot.SlotFlags & _flags) == 0)
                    continue;

                var container = _containers[i];
                if (container.ContainedEntity is { } uid)
                {
                    item = uid;
                    return true;
                }
            }

            item = default;
            slot = null;
            return false;
        }
    }
}
