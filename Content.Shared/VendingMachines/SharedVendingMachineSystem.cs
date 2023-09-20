using Content.Shared.Emag.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Containers.ItemSlots;

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
                    out VendingMachineInventoryPrototype? packPrototype) ||
                !PrototypeManager.TryIndex(packPrototype.InventoryTypePrototypeId,
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
            switch (items.Key)
            {
                case VendingMachinesInventoryTypeNames.Emagged:
                {
                    if (HasComp<EmaggedComponent>(uid))
                        inventory.AddRange(items.Value);

                    continue;
                }
                case VendingMachinesInventoryTypeNames.Contraband:
                {
                    if (component.IsContrabandEnabled)
                        inventory.AddRange(items.Value);

                    continue;
                }
                default:
                {
                    inventory.AddRange(items.Value);
                    break;
                }
            }
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

        foreach (var item in inventory)
        {
            if(!entries.ContainsKey(item.ItemId) ||
               !entries.TryGetValue(item.ItemId, out var amount))
                continue;

            // Prevent a machine's stock from going over three times
            // the prototype's normal amount. This is an arbitrary
            // number and meant to be a convenience for someone
            // restocking a machine who doesn't want to force vend out
            // all the items just to restock one empty slot without
            // losing the rest of the restock.
            item.Amount = Math.Min(item.Amount + amount, 3 * amount);
        }

        component.Items.Remove(typeId);
        component.Items.Add(typeId, inventory);
    }

    private List<VendingMachineInventoryEntry> GetInventory(string typeId,
        VendingMachineInventoryComponent component)
    {
        var empty = new List<VendingMachineInventoryEntry>();

        foreach (var itemsTypeId in component.Items.Keys)
        {
            if (typeId != itemsTypeId)
                continue;

            if (component.Items.TryGetValue(itemsTypeId, out var inventory))
            {
                return inventory;
            }
            else
            {
                return empty;
            }
        }

        return empty;
    }
}
