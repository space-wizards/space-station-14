using System.Linq;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Robust.Shared.Player;

namespace Content.Shared.Store.Systems;

public abstract partial class SharedStoreSystem
{
    private void InitializeUi()
    {
        SubscribeLocalEvent<StoreComponent, RefundEntityDeletedEvent>(OnRefundEntityDeleted);

        Subs.BuiEvents<StoreComponent>(StoreUiKey.Key,
            subs =>
            {
                subs.Event<StoreBuyListingMessage>(OnBuyRequest);
                subs.Event<StoreRequestRefundMessage>(OnRequestRefund);
                subs.Event<StoreRequestWithdrawMessage>(OnRequestWithdraw);
            });
    }

    private void OnRefundEntityDeleted(Entity<StoreComponent> ent, ref RefundEntityDeletedEvent args)
    {
        ent.Comp.BoughtEntities.Remove(args.Uid);
        DirtyField(ent.Owner, ent.Comp, nameof(StoreComponent.BoughtEntities));
    }

    /// <summary>
    /// Toggles the store Ui open and closed
    /// </summary>
    /// <param name="user">the person doing the toggling</param>
    /// <param name="store">the store being toggled</param>
    public void ToggleUi(EntityUid user, Entity<StoreComponent?> store)
    {
        if (!Resolve(store, ref store.Comp))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!Ui.TryToggleUi(store.Owner, StoreUiKey.Key, actor.PlayerSession))
            return;

        // Purely for optimization purposes, update listings only when we open this UI
        if (Ui.IsUiOpen(store.Owner, StoreUiKey.Key))
            UpdateAvailableListings(user, store);
    }

    /// <summary>
    /// Updates available listings for the user that opened this store last.
    /// Used when something in the user could change, and you need to make sure that all listings are correct.
    /// </summary>
    /// <param name="user">The person who if opening the store ui. Listings are filtered based on this.</param>
    /// <param name="store">The store entity itself</param>
    public void UpdateAvailableListings(EntityUid? user, Entity<StoreComponent?> store)
    {
        if (!Resolve(store, ref store.Comp))
            return;

        var component = store.Comp;

        //this is the person who will be passed into logic for all listing filtering.
        if (user != null) //if we have no "buyer" for this update, then don't update the listings
        {
            component.LastAvailableListings = GetAvailableListings(component.AccountOwner ?? user.Value, store, component)
                .ToHashSet();
        }

        DirtyField(store, component, nameof(StoreComponent.LastAvailableListings));
        UpdateUi((store, component));
    }

    /// <summary>
    /// Handles whenever a purchase was made.
    /// </summary>
    private void OnBuyRequest(Entity<StoreComponent> ent, ref StoreBuyListingMessage msg)
    {
        var (uid, component) = ent;
        var message = msg;

        var listing = component.FullListingsCatalog.FirstOrDefault(x => x.ID.Equals(message.Listing.Id));

        if (listing == null) //make sure this listing actually exists
        {
            Log.Debug("listing does not exist");
            return;
        }

        var buyer = msg.Actor;

        // Verify that we can actually buy this listing and it wasn't added
        if (!ListingHasCategory(listing, component.Categories))
            return;

        // Condition checking because why not
        if (listing.Conditions != null)
        {
            var args = new ListingConditionArgs(component.AccountOwner ?? GetBuyerMind(buyer), uid, listing, EntityManager);
            var conditionsMet = listing.Conditions.All(condition => condition.Condition(args));

            if (!conditionsMet)
                return;
        }

        // Check that we have enough money
        var cost = listing.Cost;
        foreach (var (currency, amount) in cost)
        {
            if (!component.Balance.TryGetValue(currency, out var balance) || balance < amount)
            {
                return;
            }
        }

        if (!IsOnStartingMap(ent))
            DisableRefund(ent);

        // Subtract the cash
        foreach (var (currency, amount) in cost)
        {
            component.Balance[currency] -= amount;

            component.BalanceSpent.TryAdd(currency, FixedPoint2.Zero);

            component.BalanceSpent[currency] += amount;
        }

        // Spawn entity
        if (listing.ProductEntity != null)
        {
            var product = PredictedSpawnAtPosition(listing.ProductEntity, Transform(buyer).Coordinates);
            Hands.PickupOrDrop(buyer, product);

            HandleRefundComp(ent, product);

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

        // Give action
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
                HandleRefundComp(ent, actionId.Value);

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
                    HandleRefundComp(ent, listing.ProductActionEntity.Value);

                return;
            }

            listing.ProductActionEntity = upgradeActionId;

            if (upgradeActionId != null)
                HandleRefundComp(ent, upgradeActionId.Value);
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

        // Log dat shit.
        _admin.Add(LogType.StorePurchase,
            LogImpact.Low,
            $"{ToPrettyString(buyer):player} purchased listing \"{ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, Proto)}\" from {ToPrettyString(uid)}");

        listing.PurchaseAmount++; //track how many times something has been purchased
        _audio.PlayPredicted(component.BuySuccessSound, msg.Actor, uid); //cha-ching!

        var buyFinished = new StoreBuyFinishedEvent
        {
            PurchasedItem = listing,
            StoreUid = uid
        };

        RaiseLocalEvent(ref buyFinished);

        DirtyField(uid, component, nameof(StoreComponent.Balance));
        DirtyField(uid, component, nameof(StoreComponent.BalanceSpent));
        DirtyField(uid, component, nameof(StoreComponent.BoughtEntities));
        DirtyField(uid, component, nameof(StoreComponent.RefundAllowed));
        UpdateUi(ent);
    }

    private void OnRequestRefund(Entity<StoreComponent> ent, ref StoreRequestRefundMessage args)
    {
        var (uid, component) = ent;

        // TODO: Remove guardian/holopara
        if (args.Actor is not { Valid: true } buyer)
            return;

        if (!IsOnStartingMap(ent))
        {
            DisableRefund(ent);
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

            PredictedDel(purchase);
        }

        component.BoughtEntities.Clear();

        foreach (var (currency, value) in component.BalanceSpent)
        {
            component.Balance[currency] += value;
        }

        // Reset store back to its original state
        RefreshAllListings(ent);
        component.BalanceSpent = new();

        DirtyField(uid, component, nameof(StoreComponent.Balance));
        DirtyField(uid, component, nameof(StoreComponent.BalanceSpent));
        DirtyField(uid, component, nameof(StoreComponent.BoughtEntities));
        UpdateUi(ent);
    }

    /// <summary>
    /// Handles dispensing the currency you requested to be withdrawn.
    /// </summary>
    /// <remarks>
    /// This would need to be done should a currency with decimal values need to use it.
    /// not quite sure how to handle that
    /// </remarks>
    private void OnRequestWithdraw(Entity<StoreComponent> ent, ref StoreRequestWithdrawMessage msg)
    {
        if (msg.Amount <= 0)
            return;

        var (uid, component) = ent;

        //make sure we have enough cash in the bank and we actually support this currency
        if (!component.Balance.TryGetValue(msg.Currency, out var currentAmount) || currentAmount < msg.Amount)
            return;

        //make sure a malicious client didn't send us random shit
        if (!Proto.TryIndex<CurrencyPrototype>(msg.Currency, out var proto))
            return;

        //we need an actually valid entity to spawn. This check has been done earlier, but just in case.
        if (proto.Cash == null || !proto.CanWithdraw)
            return;

        WithdrawCurrency(msg.Actor, proto, msg.Amount);

        component.Balance[msg.Currency] -= msg.Amount;
        DirtyField(uid, component, nameof(StoreComponent.Balance));
        UpdateUi(ent);
    }

    private void HandleRefundComp(Entity<StoreComponent> store, EntityUid purchase)
    {
        store.Comp.BoughtEntities.Add(purchase);
        var refundComp = EnsureComp<StoreRefundComponent>(purchase);
        refundComp.StoreEntity = store.Owner;
        refundComp.BoughtTime = _timing.CurTime;
    }

    private bool IsOnStartingMap(Entity<StoreComponent> store)
    {
        var xform = Transform(store);
        return store.Comp.StartingMap == xform.MapUid;
    }

    /// <summary>
    ///     Disables refunds for this store
    /// </summary>
    public void DisableRefund(Entity<StoreComponent> store)
    {
        store.Comp.RefundAllowed = false;
        DirtyField(store, store.Comp, nameof(StoreComponent.RefundAllowed));
        UpdateUi((store, store.Comp));
    }

    protected virtual void UpdateUi(Entity<StoreComponent> ent) { }

    /// <summary>
    /// Server-side method that spawns some amount of currency in hands of user.
    /// </summary>
    protected virtual void WithdrawCurrency(EntityUid user, CurrencyPrototype currency, int amount) { }
}

/// <summary>
/// Event of successfully finishing purchase in store (<see cref="SharedStoreSystem"/>.
/// </summary>
/// <param name="StoreUid">EntityUid on which store is placed.</param>
/// <param name="PurchasedItem">ListingItem that was purchased.</param>
[ByRefEvent]
public readonly record struct StoreBuyFinishedEvent(
    EntityUid StoreUid,
    ListingDataWithCostModifiers PurchasedItem
);
