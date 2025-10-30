using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Robust.Shared.Player;

namespace Content.Shared.Store.Systems;

public abstract partial class SharedStoreSystem
{
    private void InitializeUi()
    {
        SubscribeLocalEvent<StoreComponent, AfterAutoHandleStateEvent>(OnStoreAutoHandleState);

        Subs.BuiEvents<StoreComponent>(StoreUiKey.Key,
            subs =>
            {
                subs.Event<StoreBuyListingMessage>(OnBuyRequest);
                subs.Event<StoreRequestRefundMessage>(OnRequestRefund);
                subs.Event<StoreRequestWithdrawMessage>(OnRequestWithdraw);
            });
    }

    private void OnStoreAutoHandleState(Entity<StoreComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // TODO STORE this just hides some problems with prediction, such as problems with discounts, refunds and limited stock condition.
        UpdateUi(ent);
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

        Ui.TryToggleUi(store.Owner, StoreUiKey.Key, actor.PlayerSession);
    }

    /// <summary>
    /// Handles whenever a purchase was made.
    /// </summary>
    private void OnBuyRequest(Entity<StoreComponent> ent, ref StoreBuyListingMessage msg)
    {
        var (uid, component) = ent;
        var message = msg;
        var fullListings = GetAvailableListings(uid, ent.AsNullable());

        // Get a list of all listings that this player can actually buy.
        if (!TryGetListing(fullListings, message.Listing.Id, out var listing))
        {
            // If we are here this is bad, probably a mispredict or some hacking.
            Log.Debug($"{ToPrettyString(msg.Actor)} requested to buy {msg.Listing} from {ToPrettyString(ent.Owner)} store, but it doesn't have that listing available!");
            return;
        }

        var buyer = msg.Actor;

        // Check that we have enough money
        var cost = listing.Cost;
        foreach (var (currency, amount) in cost)
        {
            if (!component.Balance.TryGetValue(currency, out var balance) || balance < amount)
                return;
        }

        // Subtract the cash
        foreach (var (currency, amount) in cost)
        {
            component.Balance[currency] -= amount;
            component.BalanceSpent.TryAdd(currency, FixedPoint2.Zero);
            component.BalanceSpent[currency] += amount;
        }

        // TODO STORE
        // If we check it on first time predicted and add it on the first tick,
        // on all next ticks LimitedStockCondition will be not available again while it really is not.
        // That's why there's an evil NetManager in the middle of nowhere, someone send help.
        if (_netMan.IsServer)
            listing.PurchaseAmount++; // Track how many times something has been purchased.

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
                    foreach (var upgradeListing in fullListings)
                    {
                        if (upgradeListing.ID != listing.ProductUpgradeId)
                            continue;

                        var modified = new ListingDataWithCostModifiers(upgradeListing)
                        {
                            ProductActionEntity = actionId.Value,
                        };
                        EnsureListingUnique(component.ListingsModifiers, modified);
                        break;
                    }
                }
            }
        }

        // Handle upgrading an action
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

        // Handle product event
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

        if (!IsOnStartingMap(ent))
            DisableRefund(ent);

        _audio.PlayPredicted(component.BuySuccessSound, msg.Actor, uid); //cha-ching!

        var buyFinished = new StoreBuyFinishedEvent
        {
            PurchasedItem = listing,
            StoreUid = uid,
        };

        RaiseLocalEvent(ref buyFinished);

        // Save everything that was changed in that listing (PurchaseAmount and whatever event subscribers had changed)
        EnsureListingUnique(component.ListingsModifiers, listing);

        DirtyFields(uid,
            component,
            null,
            nameof(StoreComponent.Balance),
            nameof(StoreComponent.BalanceSpent),
            nameof(StoreComponent.BoughtEntities),
            nameof(StoreComponent.RefundAllowed),
            nameof(StoreComponent.ListingsModifiers));
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
        //RefreshAllListings(ent);
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
        Dirty(purchase, refundComp);
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
    protected virtual void WithdrawCurrency(EntityUid buyer, CurrencyPrototype proto, FixedPoint2 amount) { }
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
