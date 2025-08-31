using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.PDA.Ringer;
using Content.Server.Revolutionary;
using Content.Server.Stack;
using Content.Server.Store.Components;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Conditions;
using Content.Shared.Store.Events;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Prometheus; //Starlight


namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem
{
    #region Starlight
    private static readonly Histogram _storePurchasesMetric = Metrics.CreateHistogram(
        "sl_store_purchases",
        "Everything bounght from a \"store\" which include ling upgrades, traitor uplinks, wizard grimoires",
        ["store_name", "purchased_item", "discounted"]
    );
    #endregion

    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ActionUpgradeSystem _actionUpgrade = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RevSupplyRiftSystem _revSupplyRift = default!; // Starlight

    private void InitializeUi()
    {
        SubscribeLocalEvent<StoreComponent, StoreRequestUpdateInterfaceMessage>(OnRequestUpdate);
        SubscribeLocalEvent<StoreComponent, StoreBuyListingMessage>(OnBuyRequest);
        SubscribeLocalEvent<StoreComponent, StoreRequestWithdrawMessage>(OnRequestWithdraw);
        SubscribeLocalEvent<StoreComponent, StoreRequestRefundMessage>(OnRequestRefund);
        SubscribeLocalEvent<StoreComponent, RefundEntityDeletedEvent>(OnRefundEntityDeleted);
    }

    private void OnRefundEntityDeleted(Entity<StoreComponent> ent, ref RefundEntityDeletedEvent args)
    {
        ent.Comp.BoughtEntities.Remove(args.Uid);
    }

    /// <summary>
    /// Toggles the store Ui open and closed
    /// </summary>
    /// <param name="user">the person doing the toggling</param>
    /// <param name="storeEnt">the store being toggled</param>
    /// <param name="component"></param>
    public void ToggleUi(EntityUid user, EntityUid storeEnt, StoreComponent? component = null)
    {
        if (!Resolve(storeEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!_ui.TryToggleUi(storeEnt, StoreUiKey.Key, actor.PlayerSession))
            return;

        UpdateUserInterface(user, storeEnt, component);
    }

    /// <summary>
    /// Closes the store UI for everyone, if it's open
    /// </summary>
    public void CloseUi(EntityUid uid, StoreComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _ui.CloseUi(uid, StoreUiKey.Key);
    }

    /// <summary>
    /// STARLIGHT: Updates the user interface for a store and refreshes the listings
    /// </summary>
    /// <param name="user">The person who if opening the store ui. Listings are filtered based on this.</param>
    /// <param name="store">The store entity itself</param>
    /// <param name="component">The store component being refreshed.</param>
    public void UpdateUserInterface(EntityUid? user, EntityUid store, StoreComponent? component = null)
    {
        if (!Resolve(store, ref component))
            return;

        // STARLIGHT: Check if a rift has been destroyed and update the listing accordingly
        // This ensures the rift listing remains unavailable even when the UI is refreshed
        _revSupplyRift.CheckRiftDestroyedAndUpdateListing(component);

        //this is the person who will be passed into logic for all listing filtering.
        if (user != null) //if we have no "buyer" for this update, then don't update the listings
        {
            component.LastAvailableListings = GetAvailableListings(component.AccountOwner ?? user.Value, store, component)
                .ToHashSet();
        }

        //dictionary for all currencies, including 0 values for currencies on the whitelist
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> allCurrency = new();
        foreach (var supported in component.CurrencyWhitelist)
        {
            allCurrency.Add(supported, FixedPoint2.Zero);

            if (component.Balance.TryGetValue(supported, out var value))
                allCurrency[supported] = value;
        }

        // TODO: if multiple users are supposed to be able to interact with a single BUI & see different
        // stores/listings, this needs to use session specific BUI states.

        // only tell operatives to lock their uplink if it can be locked
        var showFooter = HasComp<RingerUplinkComponent>(store);

        var state = new StoreUpdateState(component.LastAvailableListings, allCurrency, showFooter, component.RefundAllowed);
        _ui.SetUiState(store, StoreUiKey.Key, state);
    }

    private void OnRequestUpdate(EntityUid uid, StoreComponent component, StoreRequestUpdateInterfaceMessage args)
    {
        UpdateUserInterface(args.Actor, GetEntity(args.Entity), component);
    }

    private void BeforeActivatableUiOpen(EntityUid uid, StoreComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(args.User, uid, component);
    }

    /// <summary>
    /// Handles whenever a purchase was made.
    /// </summary>
    private void OnBuyRequest(EntityUid uid, StoreComponent component, StoreBuyListingMessage msg)
    {
        var listing = component.FullListingsCatalog.FirstOrDefault(x => x.ID.Equals(msg.Listing.Id));

        if (listing == null) //make sure this listing actually exists
        {
            Log.Debug("listing does not exist");
            return;
        }

        var buyer = msg.Actor;

        // STARLIGHT: Raise an event to allow other systems to potentially cancel this purchase
        var purchaseAttemptEvent = new StorePurchaseAttemptEvent(listing.ID, uid, buyer);
        RaiseLocalEvent(ref purchaseAttemptEvent);

        // STARLIGHT: If the event handler requested cancellation, cancel the purchase
        if (purchaseAttemptEvent.Cancel)
            return;

        //verify that we can actually buy this listing and it wasn't added
        if (!ListingHasCategory(listing, component.Categories))
            return;

        //condition checking because why not
        if (listing.Conditions != null)
        {
            var args = new ListingConditionArgs(component.AccountOwner ?? GetBuyerMind(buyer), uid, listing, EntityManager);
            var conditionsMet = listing.Conditions.All(condition => condition.Condition(args));

            if (!conditionsMet)
                return;
        }

        // Starlight: Check if the listing is unavailable (e.g., out of stock)
        // We still want to show the listing in the UI, but prevent purchase
        if (listing.Unavailable)
            return;

        //check that we have enough money
        var cost = listing.Cost;
        foreach (var (currency, amount) in cost)
        {
            if (!component.Balance.TryGetValue(currency, out var balance) || balance < amount)
            {
                return;
            }
        }

        if (!IsOnStartingMap(uid, component))
            DisableRefund(uid, component);

        //subtract the cash
        foreach (var (currency, amount) in cost)
        {
            component.Balance[currency] -= amount;

            component.BalanceSpent.TryAdd(currency, FixedPoint2.Zero);

            component.BalanceSpent[currency] += amount;
        }

        //spawn entity
        if (listing.ProductEntity != null)
        {
            var product = Spawn(listing.ProductEntity, Transform(buyer).Coordinates);
            _hands.PickupOrDrop(buyer, product);

            HandleRefundComp(uid, component, product);

            var xForm = Transform(product);

            if (xForm.ChildCount > 0)
            {
                var childEnumerator = xForm.ChildEnumerator;
                while (childEnumerator.MoveNext(out var child))
                {
                    component.BoughtEntities.Add(child);
                }
            }
        }

        //give action
        if (!string.IsNullOrWhiteSpace(listing.ProductAction))
        {
            EntityUid? actionId;
            // I guess we just allow duplicate actions?
            // Allow duplicate actions and just have a single list buy for the buy-once ones.
            if (!_mind.TryGetMind(buyer, out var mind, out _))
                actionId = _actions.AddAction(buyer, listing.ProductAction);
            else
                actionId = _actionContainer.AddAction(mind, listing.ProductAction);

            // Add the newly bought action entity to the list of bought entities
            // And then add that action entity to the relevant product upgrade listing, if applicable
            if (actionId != null)
            {
                HandleRefundComp(uid, component, actionId.Value);

                if (listing.ProductUpgradeId != null)
                {
                    foreach (var upgradeListing in component.FullListingsCatalog)
                    {
                        if (upgradeListing.ID == listing.ProductUpgradeId)
                        {
                            upgradeListing.ProductActionEntity = actionId.Value;
                            break;
                        }
                    }
                }
            }
        }

        if (listing is { ProductUpgradeId: not null, ProductActionEntity: not null })
        {
            if (listing.ProductActionEntity != null)
            {
                component.BoughtEntities.Remove(listing.ProductActionEntity.Value);
            }

            if (!_actionUpgrade.TryUpgradeAction(listing.ProductActionEntity, out var upgradeActionId))
            {
                if (listing.ProductActionEntity != null)
                    HandleRefundComp(uid, component, listing.ProductActionEntity.Value);

                return;
            }

            listing.ProductActionEntity = upgradeActionId;

            if (upgradeActionId != null)
                HandleRefundComp(uid, component, upgradeActionId.Value);
        }

        if (listing.ProductEvent != null)
        {
            if (!listing.RaiseProductEventOnUser)
                RaiseLocalEvent(listing.ProductEvent);
            else
                RaiseLocalEvent(buyer, listing.ProductEvent);
        }

        if (listing.DisableRefund)
        {
            component.RefundAllowed = false;
        }

        //log dat shit.
        // Starlight: Get the resolved name without any placeholders
        var resolvedName = ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, _proto);

        // Remove any stock count or "Out of Stock" text for the log
        if (resolvedName.Contains(" ("))
        {
            resolvedName = resolvedName.Substring(0, resolvedName.IndexOf(" ("));
        }

        _admin.Add(LogType.StorePurchase,
            LogImpact.Low,
            $"{ToPrettyString(buyer):player} purchased listing \"{resolvedName}\" from {ToPrettyString(uid)}"); // Starlight

        listing.PurchaseAmount++; //track how many times something has been purchased
        _audio.PlayEntity(component.BuySuccessSound, msg.Actor, uid); //cha-ching!

        // STARTLIGHT START: Check if this listing has a StockLimitedListingCondition
        if (listing.Conditions != null)
        {
            foreach (var condition in listing.Conditions)
            {
                if (condition is StockLimitedListingCondition stockCondition)
                {
                    // Get the buyer's name
                    var buyerName = "Unknown";
                    if (TryComp<MetaDataComponent>(buyer, out var metadata))
                    {
                        buyerName = metadata.EntityName;
                    }

                    // Update the stock count and last purchaser
                    StockLimitedListingCondition.OnItemPurchased(listing.ID, buyerName, stockCondition.StockLimit);
                    break;
                }
            }
        }

        // STARLIGHT END

        var buyFinished = new StoreBuyFinishedEvent
        {
            PurchasedItem = listing,
            StoreUid = uid
        };
        RaiseLocalEvent(ref buyFinished);

        // STARLIGHT: Raise an event to notify other systems that a purchase was completed
        var purchaseCompletedEvent = new StorePurchaseCompletedEvent(listing.ID, uid, buyer);
        RaiseLocalEvent(ref purchaseCompletedEvent);

        UpdateUserInterface(buyer, uid, component);

        // STARLIGHT START: If this was a stock-limited item, update all USSP uplink UIs
        if (listing.Conditions != null)
        {
            foreach (var condition in listing.Conditions)
            {
                if (condition is StockLimitedListingCondition)
                {
                    UpdateAllUSSPUplinkUIs();
                    break;
                }
            }
        }

        #region Starlight statistics
        var accu = 0f;
        foreach (var item in listing.Cost)
        {
            accu += item.Value.Float();
        }
        _storePurchasesMetric.WithLabels([
            Loc.GetString(component.Name),
            listing.ID,
            listing.IsCostModified.ToString()
        ]).Observe(accu);
        #endregion
    }

    /// <summary>
    /// Updates all USSP uplink UIs to ensure they show the latest stock counts and last purchaser information.
    /// </summary>
    public void UpdateAllUSSPUplinkUIs()
    {
        // Find all store components that are USSP uplinks
        var query = EntityManager.EntityQuery<StoreComponent>();
        foreach (var storeComp in query)
        {
            // Skip if this is not a USSP uplink
            if (!storeComp.CurrencyWhitelist.Contains("Telebond"))
                continue;

            // Refresh all listings to ensure they have the latest stock count and last purchaser information
            RefreshAllListings(storeComp);

            // Force a refresh of the available listings
            if (storeComp.AccountOwner != null)
            {
                storeComp.LastAvailableListings = GetAvailableListings(storeComp.AccountOwner.Value, storeComp.Owner, storeComp)
                    .ToHashSet();
            }

            // Update the UI to reflect the changes
            // We'll just update it with a null user to ensure the listings are refreshed
            // The next time someone opens the UI, they'll see the updated listings
            UpdateUserInterface(null, storeComp.Owner, storeComp);

            // Force update the UI for all currently connected sessions
            ForceUpdateUiForAllSessions(storeComp.Owner, storeComp);

            Logger.DebugS("store", $"Updated USSP uplink UI for {ToPrettyString(storeComp.Owner)}");
        }
    }

    /// <summary>
    /// Forces an update of the UI for all sessions currently viewing this store.
    /// This ensures that when stock counts or last purchaser information changes,
    /// all open UIs are immediately updated.
    /// </summary>
    private void ForceUpdateUiForAllSessions(EntityUid storeUid, StoreComponent storeComp)
    {
        // Create the UI state
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> allCurrency = new();
        foreach (var supported in storeComp.CurrencyWhitelist)
        {
            allCurrency.Add(supported, FixedPoint2.Zero);

            if (storeComp.Balance.TryGetValue(supported, out var value))
                allCurrency[supported] = value;
        }

        // Only tell operatives to lock their uplink if it can be locked
        var showFooter = HasComp<RingerUplinkComponent>(storeUid);

        var state = new StoreUpdateState(storeComp.LastAvailableListings, allCurrency, showFooter, storeComp.RefundAllowed);

        // Set the UI state - this will update all connected sessions automatically
        _ui.SetUiState(storeUid, StoreUiKey.Key, state);

        // Find all players who might have this uplink open
        var query = EntityManager.EntityQuery<ActorComponent>();
        foreach (var actor in query)
        {
            // Check if this player has the uplink implanted
            if (TryComp<SubdermalImplantComponent>(storeUid, out var implant) &&
                implant.ImplantedEntity == actor.Owner)
            {
                // Force update the UI for this player
                UpdateUserInterface(actor.Owner, storeUid, storeComp);
            }
        }
        // STARLIGHT END
    }

    /// <summary>
    /// Handles dispensing the currency you requested to be withdrawn.
    /// </summary>
    /// <remarks>
    /// This would need to be done should a currency with decimal values need to use it.
    /// not quite sure how to handle that
    /// </remarks>
    private void OnRequestWithdraw(EntityUid uid, StoreComponent component, StoreRequestWithdrawMessage msg)
    {
        if (msg.Amount <= 0)
            return;

        //make sure we have enough cash in the bank and we actually support this currency
        if (!component.Balance.TryGetValue(msg.Currency, out var currentAmount) || currentAmount < msg.Amount)
            return;

        //make sure a malicious client didn't send us random shit
        if (!_proto.TryIndex<CurrencyPrototype>(msg.Currency, out var proto))
            return;

        //we need an actually valid entity to spawn. This check has been done earlier, but just in case.
        if (proto.Cash == null || !proto.CanWithdraw)
            return;

        var buyer = msg.Actor;

        FixedPoint2 amountRemaining = msg.Amount;
        var coordinates = Transform(buyer).Coordinates;

        var sortedCashValues = proto.Cash.Keys.OrderByDescending(x => x).ToList();
        foreach (var value in sortedCashValues)
        {
            var cashId = proto.Cash[value];
            var amountToSpawn = (int)MathF.Floor((float)(amountRemaining / value));
            var ents = _stack.SpawnMultiple(cashId, amountToSpawn, coordinates);
            if (ents.FirstOrDefault() is { } ent)
                _hands.PickupOrDrop(buyer, ent);
            amountRemaining -= value * amountToSpawn;
        }

        // STARLIGHT: Play sound effect when withdrawing telebonds
        if (msg.Currency == "Telebond")
        {
            var soundPath = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");
            _audio.PlayPvs(soundPath, uid, AudioParams.Default.WithMaxDistance(3f).WithVolume(5f));
        }

        component.Balance[msg.Currency] -= msg.Amount;
        UpdateUserInterface(buyer, uid, component);
    }

    private void OnRequestRefund(EntityUid uid, StoreComponent component, StoreRequestRefundMessage args)
    {
        // TODO: Remove guardian/holopara

        if (args.Actor is not { Valid: true } buyer)
            return;

        if (!IsOnStartingMap(uid, component))
        {
            DisableRefund(uid, component);
            UpdateUserInterface(buyer, uid, component);
        }

        if (!component.RefundAllowed || component.BoughtEntities.Count == 0)
            return;

        _admin.Add(LogType.StoreRefund, LogImpact.Low, $"{ToPrettyString(buyer):player} has refunded their purchases from {ToPrettyString(uid):store}");

        for (var i = component.BoughtEntities.Count - 1; i >= 0; i--)
        {
            var purchase = component.BoughtEntities[i];

            if (!Exists(purchase))
                continue;

            component.BoughtEntities.RemoveAt(i);

            _actionContainer.RemoveAction(purchase, logMissing: false);

            Del(purchase);
        }

        component.BoughtEntities.Clear();

        foreach (var (currency, value) in component.BalanceSpent)
        {
            component.Balance[currency] += value;
        }

        // Reset store back to its original state
        RefreshAllListings(component);
        component.BalanceSpent = new();
        UpdateUserInterface(buyer, uid, component);
    }

    private void HandleRefundComp(EntityUid uid, StoreComponent component, EntityUid purchase)
    {
        component.BoughtEntities.Add(purchase);
        var refundComp = EnsureComp<StoreRefundComponent>(purchase);
        refundComp.StoreEntity = uid;
        refundComp.BoughtTime = _timing.CurTime;
    }

    private bool IsOnStartingMap(EntityUid store, StoreComponent component)
    {
        var xform = Transform(store);
        return component.StartingMap == xform.MapUid;
    }

    /// <summary>
    ///     Disables refunds for this store
    /// </summary>
    public void DisableRefund(EntityUid store, StoreComponent? component = null)
    {
        if (!Resolve(store, ref component))
            return;

        component.RefundAllowed = false;
    }
}

/// <summary>
/// Event of successfully finishing purchase in store (<see cref="StoreSystem"/>.
/// </summary>
/// <param name="StoreUid">EntityUid on which store is placed.</param>
/// <param name="PurchasedItem">ListingItem that was purchased.</param>
[ByRefEvent]
public readonly record struct StoreBuyFinishedEvent(
    EntityUid StoreUid,
    ListingDataWithCostModifiers PurchasedItem
);
