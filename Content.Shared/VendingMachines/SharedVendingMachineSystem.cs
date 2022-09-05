using Robust.Shared.Prototypes;
using System.Linq;
using static Content.Shared.VendingMachines.SharedVendingMachineComponent;

namespace Content.Shared.VendingMachines;

public abstract class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedVendingMachineComponent, ComponentInit>(OnComponentInit);;
    }

    protected virtual void OnComponentInit(EntityUid uid, SharedVendingMachineComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            return;

        AddInventoryFromPrototype(uid, packPrototype.StartingInventory, InventoryType.Regular, component);
        AddInventoryFromPrototype(uid, packPrototype.EmaggedInventory, InventoryType.Emagged, component);
        AddInventoryFromPrototype(uid, packPrototype.ContrabandInventory, InventoryType.Contraband, component);
    }

    /// <summary>
    /// Returns all of the vending machine's inventory. Only includes emagged and contraband inventories if
    /// <see cref="SharedVendingMachineComponent.Emagged"/> and <see cref="SharedVendingMachineComponent.Contraband"/>
    /// are <c>true</c> respectively.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid, SharedVendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        var inventory = new List<VendingMachineInventoryEntry>(component.Inventory.Values);

        if (component.Emagged)
            inventory.AddRange(component.EmaggedInventory.Values);

        if (component.Contraband)
            inventory.AddRange(component.ContrabandInventory.Values);

        return inventory;
    }

    public List<VendingMachineInventoryEntry> GetAvailableInventory(EntityUid uid, SharedVendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        return GetAllInventory(uid, component).Where(_ => _.Amount > 0).ToList();
    }

    private void AddInventoryFromPrototype(EntityUid uid, Dictionary<string, uint>? entries,
        InventoryType type,
        SharedVendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component) || entries == null)
        {
            return;
        }

        var inventory = new Dictionary<string, VendingMachineInventoryEntry>();

        foreach (var (id, amount) in entries)
        {
            if (_prototypeManager.HasIndex<EntityPrototype>(id))
            {
                inventory.Add(id, new VendingMachineInventoryEntry(type, id, amount));
            }
        }

        switch (type)
        {
            case InventoryType.Regular:
                component.Inventory = inventory;
                break;
            case InventoryType.Emagged:
                component.EmaggedInventory = inventory;
                break;
            case InventoryType.Contraband:
                component.ContrabandInventory = inventory;
                break;
        }
    }
}

