using Content.Shared.Emag.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System.Linq;

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
        SubscribeLocalEvent<VendingMachineInventoryComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
    }

    protected virtual void OnComponentInit(EntityUid uid, VendingMachineInventoryComponent component,
        ComponentInit args)
    {
        RestockInventoryFromPrototype(uid, component);
    }

    public void RestockInventoryFromPrototype(EntityUid uid,
        VendingMachineInventoryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        foreach (var pack in component.PackPrototypeId)
        {
            if (!PrototypeManager.TryIndex(pack,
                    out VendingMachineInventoryPrototype? packPrototype))
                continue;

            if (!PrototypeManager.TryIndex(packPrototype.InventoryTypePrototypeId,
                    out VendingMachineInventoryTypePrototype? inventoryType))
                continue;

            AddInventoryFromPrototype(uid, packPrototype.Inventory, inventoryType.ID, component);
        }
    }

    /// <summary>
    /// Returns all of the vending machine's inventory. Only includes emagged and contraband inventories if
    /// <see cref="EmaggedComponent"/> exists and <see cref="VendingMachineContrabandInventoryComponent.Contraband"/>
    /// is true are <c>true</c> respectively.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid,
        VendingMachineInventoryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        var inventory = new List<VendingMachineInventoryEntry>();

        foreach (var items in component.Items)
        {
            if (items.Key == VendingMachinesInventoryTypeNames.Emagged)
            {
                if(HasComp<EmaggedComponent>(uid))
                    inventory.AddRange(items.Value);

                continue;
            }

            if (items.Key == VendingMachinesInventoryTypeNames.Contraband)
            {
                if(component.Contraband)
                    inventory.AddRange(items.Value);

                continue;
            }

            inventory.AddRange(items.Value);
        }

        return inventory;
    }

    public List<VendingMachineInventoryEntry> GetAvailableInventory(EntityUid uid,
        VendingMachineInventoryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        return GetAllInventory(uid, component).Where(_ => _.Amount > 0).ToList();
    }

    private void AddInventoryFromPrototype(EntityUid uid,
        Dictionary<string, uint>? entries,
        string typeId,
        VendingMachineInventoryComponent? component = null)
    {
        if (!Resolve(uid, ref component) || entries == null)
        {
            return;
        }

        var inventory = GetInventory(typeId, component);

        var inventoryIsEmpty = inventory.Count == 0;

        if (inventoryIsEmpty)
        {
            foreach (var (id, amount) in entries)
            {
                inventory.Add(new VendingMachineInventoryEntry(id, typeId, amount));
            }

            component.Items.Add(typeId, inventory);

            return;
        }

        foreach (var (id, amount) in entries)
        {
            foreach (var item in inventory)
            {
                if(item.ItemId == id)
                    item.Amount = Math.Min(item.Amount + amount, 3 * amount);
            }
        }

        component.Items.Remove(typeId);
        component.Items.Add(typeId, inventory);
    }

    private List<VendingMachineInventoryEntry> GetInventory(string typeId,
        VendingMachineInventoryComponent component)
    {
        foreach (var itemsTypeId in component.Items.Keys)
        {
            if (typeId != itemsTypeId)
                continue;

            component.Items.TryGetValue(itemsTypeId, out var inventory);

            return inventory ?? new List<VendingMachineInventoryEntry>();
        }

        return new List<VendingMachineInventoryEntry>();
    }
}
