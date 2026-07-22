using System.Linq;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedUserInterfaceSystem UISystem = default!;
    [Dependency] protected IRobustRandom Randomizer = default!;
    [Dependency] private EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<VendingMachineComponent>(VendingMachineUiKey.Key, subs =>
        {
            subs.Event<VendingMachineEjectMessage>(OnInventoryEjectMessage);
        });
    }

    [SubscribeLocalEvent]
    private void OnVendingGetState(Entity<VendingMachineComponent> entity, ref ComponentGetState args)
    {
        var component = entity.Comp;

        var inventory = new Dictionary<string, VendingMachineInventoryEntry>();
        var emaggedInventory = new Dictionary<string, VendingMachineInventoryEntry>();
        var contrabandInventory = new Dictionary<string, VendingMachineInventoryEntry>();

        foreach (var weh in component.Inventory)
        {
            inventory[weh.Key] = new(weh.Value);
        }

        foreach (var weh in component.EmaggedInventory)
        {
            emaggedInventory[weh.Key] = new(weh.Value);
        }

        foreach (var weh in component.ContrabandInventory)
        {
            contrabandInventory[weh.Key] = new(weh.Value);
        }

        args.State = new VendingMachineComponentState
        {
            Inventory = inventory,
            EmaggedInventory = emaggedInventory,
            ContrabandInventory = contrabandInventory,
            Contraband = component.Contraband,
            Broken = component.Broken,
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VendingMachineComponent, VendingMachineEjectComponent>();
        var curTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var eject))
        {
            UpdateEjectState((uid, comp, eject), curTime);
        }
    }

    [SubscribeLocalEvent]
    protected virtual void OnMapInit(EntityUid uid, VendingMachineComponent component, MapInitEvent args)
    {
        RestockInventoryFromPrototype(uid, component, component.InitialStockQuality);
    }

    protected virtual void UpdateUI(Entity<VendingMachineComponent?> entity) { }

    public void RestockInventoryFromPrototype(EntityUid uid,
        VendingMachineComponent? component = null, float restockQuality = 1f)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (!ProtoMan.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            return;

        AddInventoryFromPrototype(uid, packPrototype.StartingInventory, InventoryType.Regular, component, restockQuality);
        AddInventoryFromPrototype(uid, packPrototype.EmaggedInventory, InventoryType.Emagged, component, restockQuality);
        AddInventoryFromPrototype(uid, packPrototype.ContrabandInventory, InventoryType.Contraband, component, restockQuality);
        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
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
            if (!ProtoMan.HasIndex<EntityPrototype>(id)) continue;
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

    [SubscribeLocalEvent]
    private void OnActivatableUIOpenAttempt(EntityUid uid, VendingMachineComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (component.Broken)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBreak(EntityUid uid, VendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
    {
        vendComponent.Broken = true;
        Dirty(uid, vendComponent);

        UISystem.CloseUi(uid, VendingMachineUiKey.Key);
    }
}
