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

    public override void Initialize()
    {
        base.Initialize();
        InitializeEquip();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownEquip();
    }

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
}
