using Content.Shared.Emag.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Advertise.Components;
using Content.Shared.Advertise.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Stacks;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private   readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedPointLightSystem Light = default!;
    [Dependency] private   readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private   readonly SharedSpeakOnUIClosedSystem _speakOn = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UISystem = default!;
    [Dependency] protected readonly IRobustRandom Randomizer = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedCargoSystem _cargoSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VendingMachineComponent, ComponentGetState>(OnVendingGetState);
        SubscribeLocalEvent<VendingMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VendingMachineComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(InteractUsing);

        SubscribeLocalEvent<VendingMachineRestockComponent, AfterInteractEvent>(OnAfterInteract);

        Subs.BuiEvents<VendingMachineComponent>(VendingMachineUiKey.Key, subs =>
        {
            subs.Event<VendingMachineEjectMessage>(OnInventoryEjectMessage);
        });

        Subs.BuiEvents<VendingMachineComponent>(VendingMachineUiKey.Key, subs =>
        {
            subs.Event<VendingMachineWithrawMessage>(OnWithdrawPressed);
        });
    }

    private void InteractUsing(Entity<VendingMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.IsFree || !TryComp<CashComponent>(args.Used, out var cashComp))
            return;

        var cashAmmount = _stack.GetCount(args.Used);
        ent.Comp.Credit += cashAmmount;
        PredictedQueueDel(args.Used);
        Dirty(ent);

        args.Handled = true;
    }

    private void OnWithdrawPressed(EntityUid uid, VendingMachineComponent vendComponent, VendingMachineWithrawMessage args)
    {
        if (!_receiver.IsPowered(uid) || Deleted(uid))
            return;

        if (args.Actor is not { Valid: true } actor)
            return;

        if (!IsAuthorized(uid, actor, vendComponent))
            return;

        if (vendComponent.Credit == 0)
        {
            Deny((uid, vendComponent), args.Actor);
            return;
        }

        EjectCash(uid, vendComponent.Credit, vendComponent);
        vendComponent.Credit = 0;

        Audio.PlayPredicted(vendComponent.SoundVend, uid, args.Actor);
        UpdateUI((uid, vendComponent));
        Dirty(uid, vendComponent);
    }
    protected virtual void EjectCash(EntityUid uid, int cash, VendingMachineComponent vendComponent) { }

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

        args.State = new VendingMachineComponentState()
        {
            Inventory = inventory,
            EmaggedInventory = emaggedInventory,
            ContrabandInventory = contrabandInventory,
            Contraband = component.Contraband,
            EjectEnd = component.EjectEnd,
            DenyEnd = component.DenyEnd,
            DispenseOnHitEnd = component.DispenseOnHitEnd,
            Credit = component.Credit,
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VendingMachineComponent>();
        var curTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Ejecting)
            {
                if (curTime > comp.EjectEnd)
                {
                    comp.EjectEnd = null;
                    Dirty(uid, comp);

                    EjectItem(uid, comp);
                    UpdateUI((uid, comp));
                }
            }

            if (comp.Denying)
            {
                if (curTime > comp.DenyEnd)
                {
                    comp.DenyEnd = null;
                    Dirty(uid, comp);

                    TryUpdateVisualState((uid, comp));
                }
            }

            if (comp.DispenseOnHitCoolingDown)
            {
                if (curTime > comp.DispenseOnHitEnd)
                {
                    comp.DispenseOnHitEnd = null;
                    Dirty(uid, comp);
                }
            }
        }
    }

    private void OnInventoryEjectMessage(Entity<VendingMachineComponent> entity, ref VendingMachineEjectMessage args)
    {
        if (!_receiver.IsPowered(entity.Owner) || Deleted(entity))
            return;

        if (args.Actor is not { Valid: true } actor)
            return;

        AuthorizedVend(entity.Owner, actor, args.Type, args.ID, entity.Comp);
    }

    protected virtual void OnMapInit(EntityUid uid, VendingMachineComponent component, MapInitEvent args)
    {
        RestockInventoryFromPrototype(uid, component, component.InitialStockQuality);
    }

    protected virtual void EjectItem(EntityUid uid, VendingMachineComponent? vendComponent = null, bool forceEject = false) { }

    /// <summary>
    /// Checks if the user is authorized to use this vending machine
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="sender">Entity trying to use the vending machine</param>
    /// <param name="vendComponent"></param>
    public bool IsAuthorized(EntityUid uid, EntityUid sender, VendingMachineComponent? vendComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return false;

        if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            return true;

        if (_accessReader.IsAllowed(sender, uid, accessReader) || HasComp<EmaggedComponent>(uid))
            return true;

        Popup.PopupClient(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, sender);
        Deny((uid, vendComponent), sender);
        return false;
    }

    protected VendingMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, InventoryType type, VendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        if (type == InventoryType.Emagged && HasComp<EmaggedComponent>(uid))
            return component.EmaggedInventory.GetValueOrDefault(entryId);

        if (type == InventoryType.Contraband && component.Contraband)
            return component.ContrabandInventory.GetValueOrDefault(entryId);

        return component.Inventory.GetValueOrDefault(entryId);
    }

    /// <summary>
    /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
    /// or the item doesn't exist in its inventory.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="type">The type of inventory the item is from</param>
    /// <param name="itemId">The prototype ID of the item</param>
    /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
    /// <param name="vendComponent"></param>
    public void TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, EntityUid? user = null, VendingMachineComponent? vendComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return;

        if (vendComponent.Ejecting || vendComponent.Broken || !_receiver.IsPowered(uid))
        {
            return;
        }

        var entry = GetEntry(uid, itemId, type, vendComponent);

        if (string.IsNullOrEmpty(entry?.ID))
        {
            Popup.PopupClient(Loc.GetString("vending-machine-component-try-eject-invalid-item"), user, PopupType.Large);
            Deny((uid, vendComponent));
            return;
        }

        if (entry.Amount <= 0)
        {
            Popup.PopupClient(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), user, PopupType.Large);
            Deny((uid, vendComponent));
            return;
        }

        if (!vendComponent.IsFree)
        {
            if (entry.ItemPrice > vendComponent.Credit)
            {
                Popup.PopupClient("No Cash dummy", user, PopupType.Large);
                Deny((uid, vendComponent));
                return;
            }

            AddCash(entry.ItemPrice, uid, vendComponent);

            vendComponent.Credit -= (int)entry.ItemPrice;
        }

        // Start Ejecting, and prevent users from ordering while anim playing
        vendComponent.EjectEnd = Timing.CurTime + vendComponent.EjectDelay;
        vendComponent.NextItemToEject = entry.ID;
        vendComponent.ThrowNextItem = throwItem;

        if (TryComp(uid, out SpeakOnUIClosedComponent? speakComponent))
            _speakOn.TrySetFlag((uid, speakComponent));

        entry.Amount--;
        Dirty(uid, vendComponent);
        UpdateUI((uid, vendComponent));
        TryUpdateVisualState((uid, vendComponent));
        Audio.PlayPredicted(vendComponent.SoundVend, uid, user);
    }
    protected virtual void AddCash(int value, EntityUid ent, VendingMachineComponent vendingMachineComponent) { }

    public void Deny(Entity<VendingMachineComponent?> entity, EntityUid? user = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (entity.Comp.Denying)
            return;

        entity.Comp.DenyEnd = Timing.CurTime + entity.Comp.DenyDelay;
        Audio.PlayPredicted(entity.Comp.SoundDeny, entity.Owner, user, AudioParams.Default.WithVolume(-2f));
        TryUpdateVisualState(entity);
        Dirty(entity);
    }

    protected virtual void UpdateUI(Entity<VendingMachineComponent?> entity) { }


    /// <summary>
    /// Tries to update the visuals of the component based on its current state.
    /// </summary>
    public void TryUpdateVisualState(Entity<VendingMachineComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        var finalState = VendingMachineVisualState.Normal;
        if (entity.Comp.Broken)
        {
            finalState = VendingMachineVisualState.Broken;
        }
        else if (entity.Comp.Ejecting)
        {
            finalState = VendingMachineVisualState.Eject;
        }
        else if (entity.Comp.Denying)
        {
            finalState = VendingMachineVisualState.Deny;
        }
        else if (!_receiver.IsPowered(entity.Owner))
        {
            finalState = VendingMachineVisualState.Off;
        }

        // TODO: You know this should really live on the client with netsync off because client knows the state.
        if (Light.TryGetLight(entity.Owner, out var pointlight))
        {
            var lightEnabled = finalState != VendingMachineVisualState.Broken && finalState != VendingMachineVisualState.Off;
            Light.SetEnabled(entity.Owner, lightEnabled, pointlight);
        }

        _appearanceSystem.SetData(entity.Owner, VendingMachineVisuals.VisualState, finalState);
    }

    /// <summary>
    /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="sender">Entity that is trying to use the vending machine</param>
    /// <param name="type">The type of inventory the item is from</param>
    /// <param name="itemId">The prototype ID of the item</param>
    /// <param name="component"></param>
    public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
    {
        if (IsAuthorized(uid, sender, component))
        {
            TryEjectVendorItem(uid, type, itemId, component.CanShoot, sender, component);
        }
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

    private void AddInventoryFromPrototype(EntityUid uid, List<VendingMachineInventoryEntryForPrototype>? entries,
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

        foreach (var entry in entries)
        {
            if (PrototypeManager.HasIndex<EntityPrototype>(entry.ID))
            {
                var restock = entry.Amount;
                var chanceOfMissingStock = 1 - restockQuality;

                var result = Randomizer.NextFloat(0, 1);
                if (result < chanceOfMissingStock)
                {
                    restock = (uint) Math.Floor(entry.Amount * result / chanceOfMissingStock);
                }

                if (inventory.TryGetValue(entry.ID, out var entryValue))
                    // Prevent a machine's stock from going over three times
                    // the prototype's normal amount. This is an arbitrary
                    // number and meant to be a convenience for someone
                    // restocking a machine who doesn't want to force vend out
                    // all the items just to restock one empty slot without
                    // losing the rest of the restock.
                    entry.Amount = Math.Min(entryValue.Amount + entry.Amount, 3 * restock);
                else
                    inventory.Add(entry.ID, new VendingMachineInventoryEntry(type, entry.ID, restock, entry.Price));
            }
        }
    }
}
