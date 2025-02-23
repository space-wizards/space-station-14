using Content.Shared.Emag.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly IRobustRandom Randomizer = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VendingMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VendingMachineComponent, GotEmaggedEvent>(OnEmagged);

        SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);
    }

    protected virtual void OnMapInit(EntityUid uid, VendingMachineComponent component, MapInitEvent args)
    {
        RestockInventoryFromPrototype(uid, component, component.InitialStockQuality);
    }

    public void RestockInventoryFromPrototype(EntityUid uid,
        VendingMachineComponent? component = null, float restockQuality = 1f)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (!PrototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            return;

        AddInventoryFromPrototype(uid, packPrototype.StartingInventory, InventoryType.Regular, component, restockQuality);
        AddInventoryFromPrototype(uid, packPrototype.EmaggedInventory, InventoryType.Emagged, component, restockQuality);
        AddInventoryFromPrototype(uid, packPrototype.ContrabandInventory, InventoryType.Contraband, component, restockQuality);
        Dirty(uid, component);
    }

    private void OnEmagged(EntityUid uid, VendingMachineComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        // only emag if there are emag-only items
        args.Handled = component.EmaggedInventory.Count > 0;
    }

    /// <summary>
    /// Returns all of the vending machine's inventory. Only includes emagged and contraband inventories if
    /// <see cref="EmaggedComponent"/> with the EmagType.Interaction flag exists and <see cref="VendingMachineComponent.Contraband"/> is true
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

        if (_emag.CheckFlag(uid, EmagType.Interaction))
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
        VendingMachineComponent? component = null, float restockQuality = 1.0f)
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
                var restock = amount;
                var chanceOfMissingStock = 1 - restockQuality;

                var result = Randomizer.NextFloat(0, 1);
                if (result < chanceOfMissingStock)
                {
                    restock = (uint) Math.Floor(amount * result / chanceOfMissingStock);
                }

                if (inventory.TryGetValue(id, out var entry))
                    // Prevent a machine's stock from going over three times
                    // the prototype's normal amount. This is an arbitrary
                    // number and meant to be a convenience for someone
                    // restocking a machine who doesn't want to force vend out
                    // all the items just to restock one empty slot without
                    // losing the rest of the restock.
                    entry.Amount = Math.Min(entry.Amount + amount, 3 * restock);
                else
                    inventory.Add(id, new VendingMachineInventoryEntry(type, id, restock));
            }
        }
    }
}
