using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Inventory.Events;
using Content.Shared.Storage;
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
        SubscribeAllEvent<OpenSlotStorageNetworkMessage>(OnOpenSlotStorage);

        _vvm.GetTypeHandler<InventoryComponent>()
            .AddHandler(HandleViewVariablesSlots, ListViewVariablesSlots);

        SubscribeLocalEvent<InventoryComponent, AfterAutoHandleStateEvent>(AfterAutoState);
    }

    private void ShutdownSlots()
    {
        _vvm.GetTypeHandler<InventoryComponent>()
            .RemoveHandler(HandleViewVariablesSlots, ListViewVariablesSlots);
    }

    /// <summary>
    /// Tries to find an entity in the specified slot with the specified component.
    /// </summary>
    public bool TryGetInventoryEntity<T>(Entity<InventoryComponent?> entity, out Entity<T?> target)
        where T : IComponent, IClothingSlots
    {
        if (TryGetContainerSlotEnumerator(entity.Owner, out var containerSlotEnumerator))
        {
            while (containerSlotEnumerator.NextItem(out var item, out var slot))
            {
                if (!TryComp<T>(item, out var required))
                    continue;

                if ((((IClothingSlots) required).Slots & slot.SlotFlags) == 0x0)
                    continue;

                target = (item, required);
                return true;
            }
        }

        target = EntityUid.Invalid;
        return false;
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

    private void AfterAutoState(Entity<InventoryComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateInventoryTemplate(ent);
    }

    protected virtual void UpdateInventoryTemplate(Entity<InventoryComponent> ent)
    {
        if (ent.Comp.LifeStage < ComponentLifeStage.Initialized)
            return;

        if (!_prototypeManager.TryIndex(ent.Comp.TemplateId, out InventoryTemplatePrototype? invTemplate))
            return;

        DebugTools.Assert(ent.Comp.Slots.Length == invTemplate.Slots.Length);

        ent.Comp.Slots = invTemplate.Slots;

        var ev = new InventoryTemplateUpdated();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnOpenSlotStorage(OpenSlotStorageNetworkMessage ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        if (TryGetSlotEntity(uid, ev.Slot, out var entityUid) && TryComp<StorageComponent>(entityUid, out var storageComponent))
        {
            _storageSystem.OpenStorageUI(entityUid.Value, uid, storageComponent, false);
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

        if (!_containerSystem.TryGetContainer(uid, slotDefinition.Name, out var container, containerComp))
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
        if (!Resolve(entity.Owner, ref entity.Comp, false))
        {
            containerSlotEnumerator = default;
            return false;
        }

        containerSlotEnumerator = new InventorySlotEnumerator(entity.Comp, flags);
        return true;
    }

    public InventorySlotEnumerator GetSlotEnumerator(Entity<InventoryComponent?> entity, SlotFlags flags = SlotFlags.All)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
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
    /// Change the inventory template ID an entity is using. The new template must be compatible.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For an inventory template to be compatible with another, it must have exactly the same slot names.
    /// All other changes are rejected.
    /// </para>
    /// </remarks>
    /// <param name="ent">The entity to update.</param>
    /// <param name="newTemplate">The ID of the new inventory template prototype.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the new template is not compatible with the existing one.
    /// </exception>
    public void SetTemplateId(Entity<InventoryComponent> ent, ProtoId<InventoryTemplatePrototype> newTemplate)
    {
        var newPrototype = _prototypeManager.Index(newTemplate);

        if (!newPrototype.Slots.Select(x => x.Name).SequenceEqual(ent.Comp.Slots.Select(x => x.Name)))
            throw new ArgumentException("Incompatible inventory template!");

        ent.Comp.TemplateId = newTemplate;
        Dirty(ent);
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
