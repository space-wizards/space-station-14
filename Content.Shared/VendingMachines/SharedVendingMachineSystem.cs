using Content.Shared.Emag.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Containers;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public const string ContainerName = "vending_storage";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VendingMachineInventoryComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
    }

    protected virtual void OnComponentInit(EntityUid uid, VendingMachineInventoryComponent? component,
        ComponentInit args)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        component.Storage = _container.EnsureContainer<Container>(uid, ContainerName);

        RestockInventoryFromPrototype(uid, component);
    }

    /// <summary>
    /// Updating the content in the machine
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
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
    /// Checking for inventory. If it is missing,
    /// it is created anew with all the items originally
    /// added to the pack.
    ///Otherwise, upgrade items to a certain number.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="items"></param>
    /// <param name="typeId"></param>
    /// <param name="component"></param>
    private void AddInventoryFromPrototype(EntityUid uid,
        Dictionary<string, uint>? items,
        string typeId,
        VendingMachineInventoryComponent? component = null)
    {
        if (!Resolve(uid, ref component) || items == null)
        {
            return;
        }

        var inventory = GetInventoryByType(typeId, component);
        var inventoryIsEmpty = inventory.Count == 0;

        if (inventoryIsEmpty)
        {
            StockInventory(uid, items, typeId, component);

            return;
        }

        RestockInventory(uid, items, component, inventory);
    }

    /// <summary>
    /// Complete creation of inventory from the pack
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="items"></param>
    /// <param name="typeId"></param>
    /// <param name="component"></param>
    private void StockInventory(EntityUid uid,
        Dictionary<string, uint> items,
        string typeId,
        VendingMachineInventoryComponent component)
    {
        var inventory = new List<VendingMachineInventoryEntry>();

        foreach (var (prototypeId, amount) in items)
        {
            var entry = CreateEntryToInventory(prototypeId, typeId, amount, component, inventory);

            CreateEntity(uid, prototypeId, amount, component, entry);
        }
    }

    /// <summary>
    /// Recreating inventory from a pack
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="items"></param>
    /// <param name="component"></param>
    /// <param name="inventory"></param>
    private void RestockInventory(EntityUid uid,
        Dictionary<string, uint> items,
        VendingMachineInventoryComponent component,
        List<VendingMachineInventoryEntry> inventory)
    {
        foreach (var item in inventory)
        {
            if(!items.ContainsKey(item.PrototypeId) ||
               !items.TryGetValue(item.PrototypeId, out var amount))
                continue;

            var oldAmount = item.Amount;

            // Prevent a machine's stock from going over three times
            // the prototype's normal amount. This is an arbitrary
            // number and meant to be a convenience for someone
            // restocking a machine who doesn't want to force vend out
            // all the items just to restock one empty slot without
            // losing the rest of the restock.
            item.Amount = Math.Min(item.Amount + amount, 3 * amount);

            if (oldAmount == item.Amount)
                continue;

            var amountDifference = item.Amount - oldAmount;

            CreateEntity(uid, item.PrototypeId, amountDifference, component, item);
        }
    }

    /// <summary>
    /// Returns all of the vending machine's inventory. Only includes emagged and contraband inventories if
    /// <see cref="EmaggedComponent"/> exists and <see cref="VendingMachineInventoryComponent.Contraband"/>
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

        foreach (var items in component.Inventories)
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

    /// <summary>
    /// Getting items by inventory type
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    private List<VendingMachineInventoryEntry> GetInventoryByType(string typeId,
        VendingMachineInventoryComponent component)
    {
        foreach (var inventoryTypeId in component.Inventories.Keys)
        {
            if (typeId != inventoryTypeId)
                continue;

            component.Inventories.TryGetValue(inventoryTypeId, out var inventory);

            return inventory ?? new List<VendingMachineInventoryEntry>();
        }

        return new List<VendingMachineInventoryEntry>();
    }

    /// <summary>
    /// Getting data of all items, in an amount greater than zero
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public List<VendingMachineInventoryEntry> GetAvailableInventory(EntityUid uid,
        VendingMachineInventoryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        return GetAllInventory(uid, component).Where(_ => _.Amount > 0).ToList();
    }

    /// <summary>
    /// Entry - "item" in the inventory.
    /// Just the data that will be needed to work with entity
    /// </summary>
    /// <param name="prototypeId"></param>
    /// <param name="typeId"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    /// <param name="inventory"></param>
    /// <returns></returns>
    private VendingMachineInventoryEntry CreateEntryToInventory(string prototypeId,
        string typeId,
        uint amount,
        VendingMachineInventoryComponent component,
        List<VendingMachineInventoryEntry> inventory)
    {
        var entry = new VendingMachineInventoryEntry(prototypeId, typeId, amount);

        component.Inventories.Add(typeId, inventory);
        inventory.Add(entry);

        return entry;
    }

    /// <summary>
    /// A specific implementation of the subject in the form of an entity.
    /// When creating, we always store it in a container
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="prototypeId"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    /// <param name="entry"></param>
    private void CreateEntity(EntityUid uid,
        string prototypeId,
        uint amount,
        VendingMachineInventoryComponent? component,
        VendingMachineInventoryEntry entry)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        for (var i = 0; i < amount; i++)
        {
            var entityUid = Spawn(prototypeId, Transform(uid).Coordinates);

            entry.Uids.Add(entityUid);
            component.Storage.Insert(entityUid, EntityManager);
        }
    }
}
