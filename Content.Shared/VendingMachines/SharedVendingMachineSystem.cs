using Content.Shared.Emag.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VendingMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
    }

    protected virtual void OnComponentInit(EntityUid uid, VendingMachineComponent component, ComponentInit args)
    {
        RestockInventoryFromPrototype(uid, component);
    }

    public void RestockInventoryFromPrototype(EntityUid uid,
        VendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (!PrototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            return;

        AddInventoryFromPrototype(uid, packPrototype.StartingInventory, InventoryType.Regular, component);
        AddInventoryFromPrototype(uid, packPrototype.EmaggedInventory, InventoryType.Emagged, component);
        AddInventoryFromPrototype(uid, packPrototype.ContrabandInventory, InventoryType.Contraband, component);
    }

    /// <summary>
    /// Returns all of the vending machine's inventory. Only includes emagged and contraband inventories if
    /// <see cref="EmaggedComponent"/> exists and <see cref="VendingMachineComponent.Contraband"/> is true
    /// are <c>true</c> respectively.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid, VendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        var inventory = new List<VendingMachineInventoryEntry>(component.Inventory.Values);

        if (HasComp<EmaggedComponent>(uid))
            inventory.AddRange(component.EmaggedInventory.Values);

        if (component.Contraband)
            inventory.AddRange(component.ContrabandInventory.Values);

        return inventory;
    }

    public List<VendingMachineInventoryEntry> GetAvailableInventory(EntityUid uid, VendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        return GetAllInventory(uid, component).Where(_ => _.Amount > 0).ToList();
    }

    private void AddInventoryFromPrototype(EntityUid uid, Dictionary<string, uint>? entries,
        InventoryType type,
        VendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component) || entries == null)
        {
            return;
        }

        Dictionary<string, VendingMachineInventoryEntry> inventory;
        switch (type)
        {
            case InventoryType.Regular:
                inventory = component.Inventory;
                break;
            case InventoryType.Emagged:
                inventory = component.EmaggedInventory;
                break;
            case InventoryType.Contraband:
                inventory = component.ContrabandInventory;
                break;
            default:
                return;
        }

        foreach (var (id, amount) in entries)
        {
            if (PrototypeManager.HasIndex<EntityPrototype>(id))
            {
                if (inventory.TryGetValue(id, out var entry))
                    // Prevent a machine's stock from going over three times
                    // the prototype's normal amount. This is an arbitrary
                    // number and meant to be a convenience for someone
                    // restocking a machine who doesn't want to force vend out
                    // all the items just to restock one empty slot without
                    // losing the rest of the restock.
                    entry.Amount = Math.Min(entry.Amount + amount, 3 * amount);
                else
                    inventory.Add(id, new VendingMachineInventoryEntry(type, id, amount));
            }
        }
    }
}
