using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Vocalization.Systems;
using Content.Shared.Cargo;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Emp;
using Content.Shared.Power;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines;
using Content.Shared.Wall;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
// 🌟Starlight🌟 
using Content.Server.Economy;
using Content.Shared.Economy;
using Content.Shared.Emag.Components;
using Content.Shared.Tag; 
using Content.Shared.Cargo.Components; 
using Content.Server.Administration.Managers; 
using Content.Shared.Administration.Logs; // Starlight-edit
using Content.Shared.Database;
using Robust.Shared.Player; // Starlight-edit

namespace Content.Server.VendingMachines
{
    public sealed class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        // 🌟Starlight🌟 start 
        [Dependency] private readonly ItemPriceManager _itemPriceManager = default!; 
        [Dependency] private readonly IComponentFactory _componentFactory = default!; 
        [Dependency] private readonly IPlayerRolesManager _playerRolesManager = default!; 
        [Dependency] private readonly TagSystem _tag = default!; 
        [Dependency] private readonly CargoSystem _cargoSystem = default!;
        [Dependency] private readonly Content.Server.Station.Systems.StationSystem _stationSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        // 🌟Starlight🌟 end 

        private const float WallVendEjectDistanceFromWall = 1f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<VendingMachineComponent, PriceCalculationEvent>(OnVendingPrice);
            SubscribeLocalEvent<VendingMachineComponent, EmpPulseEvent>(OnEmpPulse);
            SubscribeLocalEvent<VendingMachineComponent, TryVocalizeEvent>(OnTryVocalize);

            SubscribeLocalEvent<VendingMachineComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);

            SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);

            // 🌟Starlight🌟 Push balance immediately when the UI opens so the client doesn't have to wait for a request/refresh
            // Because im gonna loose this shit with calling from client
            SubscribeLocalEvent<VendingMachineComponent, BoundUIOpenedEvent>(OnUiOpened);
        }
        private void OnVendingPrice(EntityUid uid, VendingMachineComponent component, ref PriceCalculationEvent args)
        {
            var price = 0.0;

            foreach (var entry in component.Inventory.Values)
            {
                if (!PrototypeManager.TryIndex<EntityPrototype>(entry.ID, out var proto))
                {
                    Log.Error($"Unable to find entity prototype {entry.ID} on {ToPrettyString(uid)} vending.");
                    continue;
                }

                price += entry.Amount * _pricing.GetEstimatedPrice(proto);
            }

            args.Price += price;
        }

        protected override void OnMapInit(EntityUid uid, VendingMachineComponent component, MapInitEvent args)
        {
            base.OnMapInit(uid, component, args);

            if (HasComp<ApcPowerReceiverComponent>(uid))
            {
                TryUpdateVisualState((uid, component));
            }

            //🌟Starlight🌟 Persist prices so UI shows them on first open
            PersistInventoryPrices(uid, component);
        }

        private void OnActivatableUIOpenAttempt(EntityUid uid, VendingMachineComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Broken)
                args.Cancel();
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, ref PowerChangedEvent args)
        {
            TryUpdateVisualState((uid, component));
        }

        private void OnBreak(EntityUid uid, VendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
        {
            vendComponent.Broken = true;
            TryUpdateVisualState((uid, vendComponent));
        }

        private void OnDamageChanged(EntityUid uid, VendingMachineComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased && component.Broken)
            {
                component.Broken = false;
                TryUpdateVisualState((uid, component));
                return;
            }

            if (component.Broken || component.DispenseOnHitCoolingDown ||
                component.DispenseOnHitChance == null || args.DamageDelta == null)
                return;

            if (args.DamageIncreased && args.DamageDelta.GetTotal() >= component.DispenseOnHitThreshold &&
                _random.Prob(component.DispenseOnHitChance.Value))
            {
                if (component.DispenseOnHitCooldown != null)
                {
                    component.DispenseOnHitEnd = Timing.CurTime + component.DispenseOnHitCooldown.Value;
                }

                EjectRandom(uid, throwItem: true, forceEject: true, component);
            }
        }

        private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            EjectRandom(uid, throwItem: true, forceEject: false, component);
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.CanShoot"/> property of the vending machine.
        /// </summary>
        public void SetShooting(EntityUid uid, bool canShoot, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.CanShoot = canShoot;
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.Contraband"/> property of the vending machine.
        /// </summary>
        public void SetContraband(EntityUid uid, bool contraband, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Contraband = contraband;
            Dirty(uid, component);
        }

        /// <summary>
        /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
        /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
        /// <param name="vendComponent"></param>
        public void EjectRandom(EntityUid uid, bool throwItem, bool forceEject = false, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var availableItems = GetAvailableInventory(uid, vendComponent);
            if (availableItems.Count <= 0)
                return;

            var item = _random.Pick(availableItems);

            if (forceEject)
            {
                vendComponent.NextItemToEject = item.ID;
                vendComponent.ThrowNextItem = throwItem;
                var entry = GetEntry(uid, item.ID, item.Type, vendComponent);
                if (entry != null)
                    entry.Amount--;
                EjectItem(uid, vendComponent, forceEject);
            }
            else
            {
                TryEjectVendorItem(uid, item.Type, item.ID, throwItem, user: null, vendComponent: vendComponent);
            }
        }

        protected override void EjectItem(EntityUid uid, VendingMachineComponent? vendComponent = null, bool forceEject = false)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            // No need to update the visual state because we never changed it during a forced eject
            if (!forceEject)
                TryUpdateVisualState((uid, vendComponent));

            if (string.IsNullOrEmpty(vendComponent.NextItemToEject))
            {
                vendComponent.ThrowNextItem = false;
                return;
            }

            // Default spawn coordinates
            var xform = Transform(uid);
            var spawnCoordinates = xform.Coordinates;

            //Make sure the wallvends spawn outside of the wall.
            if (TryComp<WallMountComponent>(uid, out var wallMountComponent))
            {
                var offset = (wallMountComponent.Direction + xform.LocalRotation - Math.PI / 2).ToVec() * WallVendEjectDistanceFromWall;
                spawnCoordinates = spawnCoordinates.Offset(offset);
            }
            // Starlight-edit start:
            var itemProto = vendComponent.NextItemToEject;
            var ent = Spawn(itemProto, spawnCoordinates);
            // Starlight-edit end:

            if (vendComponent.ThrowNextItem)
            {
                var range = vendComponent.NonLimitedEjectRange;
                var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                _throwingSystem.TryThrow(ent, direction, vendComponent.NonLimitedEjectForce);
            }

             // Starlight-start
            // Perform idempotent debit and cargo credit after the item is spawned
            // Only charge if prices are shown, not emagged, we have a buyer, and not charged yet this operation
            var buyer = vendComponent.LastBuyer;
            var isEmagged = HasComp<EmaggedComponent>(uid);
            
            if (!isEmagged && vendComponent.ShowPrices && buyer is { } buyerUid && !vendComponent.DebitApplied && itemProto != null)
            {
                var entry = GetEntry(uid, itemProto, vendComponent.CurrentItemType, vendComponent);
                var price = entry?.Price ?? 0;
                if (price > 0)
                {
                    var playerData = _playerRolesManager.GetPlayerData(buyerUid);
                    if (playerData != null)
                    {
                        // Double-check sufficient funds
                        if (playerData.Balance >= price)
                        {
                            playerData.Balance -= price;
                            vendComponent.DebitApplied = true;
                            Popup.PopupEntity($"Debited {price}\u20a1. Balance: {playerData.Balance}\u20a1", uid, buyerUid);
                            SendBalanceUpdate(uid, buyerUid, playerData.Balance);

                            // Alogs
                            _adminLogger.Add(
                                LogType.Action,
                                LogImpact.Medium,
                                $"{ToPrettyString(buyerUid):player} bought {ToPrettyString(ent):entity} for {price}₡ from {ToPrettyString(uid):entity} Balance left: {playerData.Balance}₡");

                            // Credit cargo 10x price
                            var stationUid = _stationSystem.GetOwningStation(uid);

                            if (stationUid != null && TryComp<StationBankAccountComponent>(stationUid, out var bank))
                            {
                                var creditLong = (long)price * 10L;
                                var toCredit = (int)Math.Clamp(creditLong, int.MinValue, int.MaxValue);
                                if (toCredit > 0)
                                    _cargoSystem.UpdateBankAccount((stationUid.Value, bank), toCredit, bank.PrimaryAccount);
                            }

                        }
                        else
                        {
                            Popup.PopupEntity($"Insufficient funds. Required: {price}\u20a1", uid, buyerUid);
                        }
                    }
                }
            }
            // Starlight-end

            vendComponent.NextItemToEject = null;
            vendComponent.ThrowNextItem = false;
            vendComponent.LastBuyer = null; // Starlight-edit
            vendComponent.DebitApplied = false; // Starlight-edit
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent>();
            while (disabled.MoveNext(out var uid, out _, out var comp))
            {
                if (comp.NextEmpEject < Timing.CurTime)
                {
                    EjectRandom(uid, true, false, comp);
                    comp.NextEmpEject += (5 * comp.EjectDelay);
                }
            }
        }

        private void OnPriceCalculation(EntityUid uid, VendingMachineRestockComponent component, ref PriceCalculationEvent args)
        {
            List<double> priceSets = new();

            // Find the most expensive inventory and use that as the highest price.
            foreach (var vendingInventory in component.CanRestock)
            {
                double total = 0;

                if (PrototypeManager.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
                {
                    foreach (var (item, amount) in inventoryPrototype.StartingInventory)
                    {
                        if (PrototypeManager.TryIndex(item, out EntityPrototype? entity))
                            total += _pricing.GetEstimatedPrice(entity) * amount;
                    }
                }

                priceSets.Add(total);
            }

            args.Price += priceSets.Max();
        }

        // 🌟Starlight🌟 Send balance to the opening player right away so it shows before any purchase
        private void OnUiOpened(EntityUid uid, VendingMachineComponent component, BoundUIOpenedEvent args)
        {
            if (!Equals(args.UiKey, VendingMachineUiKey.Key))
                return;

            if (!component.ShowPrices)
                return;

            var actor = args.Actor;
            var playerData = _playerRolesManager.GetPlayerData(actor);
            if (playerData != null)
            {
                SendBalanceUpdate(uid, actor, playerData.Balance);
            }
        }

        private void OnEmpPulse(EntityUid uid, VendingMachineComponent component, ref EmpPulseEvent args)
        {
            if (!component.Broken && this.IsPowered(uid, EntityManager))
            {
                args.Affected = true;
                args.Disabled = true;
                component.NextEmpEject = Timing.CurTime;
            }
        }

        private void OnTryVocalize(Entity<VendingMachineComponent> ent, ref TryVocalizeEvent args)
        {
            args.Cancelled |= ent.Comp.Broken;
        }

        #region 🌟Starlight🌟 
        /// <summary>
        /// Persist prices into the live component so clients have prices on first open
        /// </summary>
        private void PersistInventoryPrices(EntityUid uid, VendingMachineComponent component)
        {
        var isEmagged = HasComp<EmaggedComponent>(uid);
            void PriceDict(Dictionary<string, VendingMachineInventoryEntry> dict)
            {
                foreach (var entry in dict.Values)
                {
            if (!component.ShowPrices || isEmagged)
                    {
                        entry.Price = 0;
                        continue;
                    }

                    if (PrototypeManager.TryIndex<EntityPrototype>(entry.ID, out var proto) &&
                        proto.TryGetComponent<ItemPriceComponent>(out var priceComponent, _componentFactory))
                    {
                        var categoryPrice = _itemPriceManager.GetPriceForPrototype(entry.ID, priceComponent.PriceCategory);
                        entry.Price = categoryPrice ?? priceComponent.FallbackPrice;
                    }
                    else
                    {
                        if (PrototypeManager.TryIndex<EntityPrototype>(entry.ID, out var p2))
                        {
                            var guessed = GuessCategory(p2);
                            if (guessed != null)
                            {
                                var catPrice = _itemPriceManager.GetPriceForPrototype(entry.ID, guessed);
                                if (catPrice.HasValue)
                                {
                                    entry.Price = catPrice.Value;
                                    continue;
                                }
                            }

                            var est = _pricing.GetEstimatedPrice(p2);
                            entry.Price = Math.Max(1, (int) Math.Round(est));
                        }
                        else
                        {
                            entry.Price = 1;
                        }
                    }
                }
            }

            PriceDict(component.Inventory);
            PriceDict(component.EmaggedInventory);
            PriceDict(component.ContrabandInventory);

            Dirty(uid, component);
        }

        /// <summary>
        /// Override to calculate prices for vending machine inventory entries using ItemPriceManager
        /// </summary>
        protected override void CalculateInventoryPrices(Dictionary<string, VendingMachineInventoryEntry> inventory, bool showPrices)
        {
            foreach (var entry in inventory.Values)
            {
                if (!showPrices)
                {
                    entry.Price = 0; // Free items for machines
                    continue;
                }

                if (!PrototypeManager.TryIndex<EntityPrototype>(entry.ID, out var proto))
                {
                    entry.Price = 1;
                    continue;
                }

                // Try to get price from ItemPriceManager using prototype
                if (proto.Components.TryGetComponent("ItemPrice", out var comp) && comp is ItemPriceComponent priceComponent)
                {
                    // Use the new GetPriceForPrototype method to ensure consistent pricing
                    var categoryPrice = _itemPriceManager.GetPriceForPrototype(entry.ID, priceComponent.PriceCategory);
                    entry.Price = categoryPrice ?? priceComponent.FallbackPrice;
                }
                else
                {
                    // Ensure a sensible fallback even if no ItemPriceComponent is present
                    var guessed = GuessCategory(proto);
                    if (guessed != null)
                    {
                        var catPrice = _itemPriceManager.GetPriceForPrototype(entry.ID, guessed);
                        if (catPrice.HasValue)
                        {
                            entry.Price = catPrice.Value;
                            continue;
                        }
                    }

                    var est = _pricing.GetEstimatedPrice(proto);
                    entry.Price = Math.Max(1, (int) Math.Round(est));
                }
            }
        }

        /// <summary>
        /// Override to handle balance requests from client
        /// </summary>
        protected override void OnRequestBalanceMessage(Entity<VendingMachineComponent> entity, ref VendingMachineRequestBalanceMessage args)
        {
            if (args.Actor is not { Valid: true } actor)
            {
                return;
            }

            var playerData = _playerRolesManager.GetPlayerData(actor);
            if (playerData != null)
            {
                SendBalanceUpdate(entity.Owner, actor, playerData.Balance);
            }
        }

        /// <summary>
        /// Sends balance update to client
        /// </summary>
        private void SendBalanceUpdate(EntityUid uid, EntityUid player, int balance)
        {
            if (TryComp<VendingMachineComponent>(uid, out var component))
            {
                UISystem.ServerSendUiMessage(uid, VendingMachineUiKey.Key,
                    new VendingMachineBalanceUpdateMessage(balance), player);
            }
        }

        public override void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
        {
            // Guard rails before any payment to avoid overcharging on spaming
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (!IsAuthorized(uid, sender, component))
                return;

            // If an ejection is already in progress or machine is broken, ignore
            if (component.Ejecting || component.Broken)
                return;

            if (!TryComp<ActorComponent>(sender, out var actor))
                return;

            // Get player data for payment
            var playerData = _playerRolesManager.GetPlayerData(sender);
            if (playerData == null)
            {
                return;
            }

            // Get inventory entry for the correct inventory bucket
            var entry = GetEntry(uid, itemId, type, component);
            if (entry == null)
                return;

            if (entry.Amount <= 0)
                return;

            var isEmagged = HasComp<EmaggedComponent>(uid);

            // If prices should be shown but this entry still has a 0 price, (likely because UI pricing
            // operated on a copied dictionary and did not persist back to the component inventory)
            // compute and persist the price now so payment logic can run
            if (!isEmagged && component.ShowPrices && entry.Price <= 0)
            {
                if (PrototypeManager.TryIndex<EntityPrototype>(itemId, out var proto) &&
                    proto.TryGetComponent<ItemPriceComponent>(out var priceComponent, _componentFactory))
                {
                    var categoryPrice = _itemPriceManager.GetPriceForPrototype(itemId, priceComponent.PriceCategory);
                    entry.Price = categoryPrice ?? priceComponent.FallbackPrice;
                }
                else
                {
                    if (PrototypeManager.TryIndex<EntityPrototype>(itemId, out var p2))
                    {
                        var guessed = GuessCategory(p2);
                        if (guessed != null)
                        {
                            var catPrice = _itemPriceManager.GetPriceForPrototype(itemId, guessed);
                            if (catPrice.HasValue)
                            {
                                entry.Price = catPrice.Value;
                            }
                            else
                            {
                                var est = _pricing.GetEstimatedPrice(p2);
                                entry.Price = Math.Max(1, (int) Math.Round(est));
                            }
                        }
                        else
                        {
                            var est = _pricing.GetEstimatedPrice(p2);
                            entry.Price = Math.Max(1, (int) Math.Round(est));
                        }
                    }
                    else
                    {
                        entry.Price = 1;
                    }
                }
            }

            // If payment is required, pre-check funds but DO NOT! deduct yet
            if (!isEmagged && component.ShowPrices && entry.Price > 0)
            {
                if (playerData.Balance < entry.Price)
                {
                    Popup.PopupEntity($"Insufficient funds. Required: {entry.Price}\u20a1", uid, sender);
                    return;
                }
            }

            // Start the ejection, debit will ocur in EjectItem after item has spawned
            TryEjectVendorItem(uid, type, itemId, component.CanShoot, sender, component);
        }

    private static readonly ProtoId<TagPrototype> _foodSnackTag = "FoodSnack";
    private static readonly ProtoId<TagPrototype> _cigaretteTag = "Cigarette";
    private static readonly ProtoId<TagPrototype> _cigarTag = "Cigar";

        private string? GuessCategory(EntityPrototype proto)
        {
            // Prefer tags if present on the prototype
            if (proto.TryGetComponent<TagComponent>(out var tagComp, _componentFactory))
            {
                if (_tag.HasTag(tagComp, _foodSnackTag))
                    return "food_cheap";
                if (_tag.HasTag(tagComp, _cigaretteTag) || _tag.HasTag(tagComp, _cigarTag))
                    return "cigaretes";
            }

            // Heuristic on prototype ID
            var id = proto.ID.ToLowerInvariant();
            if (id.Contains("cig"))
                return "cigaretes";
            if (id.Contains("cola") || id.Contains("drink") || id.Contains("soda") || id.Contains("beer") || id.Contains("juice"))
                return "drink";
            if (id.Contains("snack") || id.Contains("chips") || id.Contains("donk") || id.Contains("candy") || id.Contains("bar"))
                return "food_cheap";

            if (proto.TryGetComponent<Content.Shared.Clothing.Components.ClothingComponent>(out _, _componentFactory))
                return "clothing";
            if (id.StartsWith("clothing") || id.Contains("uniform") || id.Contains("shoes") || id.Contains("gloves") || id.Contains("belt") || id.Contains("backpack") || id.Contains("hat") || id.Contains("mask") || id.Contains("coat") || id.Contains("jacket"))
                return "clothing";

            return null;
        }
    #endregion
    }
}
