using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Mind.Components;
using Content.Server.Store.Components;
using Content.Server.UserInterface;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Store;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    public void InitializeUi()
    {
        SubscribeLocalEvent<StoreComponent, StoreRequestUpdateInterfaceMessage>((_,c,r) => UpdateUserInterface(r.CurrentBuyer, c));
        SubscribeLocalEvent<StoreComponent, StoreBuyListingMessage>(OnBuyRequest);
    }

    public void ToggleUi(EntityUid user, StoreComponent component)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        var ui = component.Owner.GetUIOrNull(StoreUiKey.Key);
        ui?.Toggle(actor.PlayerSession);

        UpdateUserInterface(user, component, ui);
    }

    public void UpdateUserInterface(EntityUid? user, StoreComponent component, BoundUserInterface? ui = null)
    {
        if (ui == null)
        {
            ui = component.Owner.GetUIOrNull(StoreUiKey.Key);
            if (ui == null)
            {
                Logger.Error("No Ui key.");
                return;
            }
        }

        //this is the person who will be passed into logic for all listing filtering.
        var buyer = user;
        if (buyer != null) //if we have no "buyer" for this update, then don't update the listings
        {
            if (component.AccountOwner != null) //if we have one stored, then use that instead
                buyer = component.AccountOwner.Value;

            component.LastAvailableListings = GetAvailableListings(buyer.Value, component).ToHashSet();
        }

        //dictionary for all currencies, including 0 values for currencies on the whitelist
        Dictionary<string, FixedPoint2> allCurrency = new();
        foreach (var supported in component.CurrencyWhitelist)
        {
            allCurrency.Add(supported, FixedPoint2.Zero);

            if (component.Balance.ContainsKey(supported))
                allCurrency[supported] = component.Balance[supported];
        }

        var state = new StoreUpdateState(buyer, component.LastAvailableListings, allCurrency);
        ui.SetState(state);
    }

    private void OnBuyRequest(EntityUid uid, StoreComponent component, StoreBuyListingMessage msg)
    {
        //verify that we can actually buy this listing and it wasn't added
        if (!ListingHasCategory(msg.Listing, component.Categories))
            return;

        if (msg.Listing.Conditions != null)
        {
            var args = new ListingConditionArgs(msg.Buyer, msg.Listing, EntityManager);
            var conditionsMet = true;

            foreach (var condition in msg.Listing.Conditions)
                if (!condition.Condition(args))
                    conditionsMet = false;

            if (!conditionsMet)
                return;
        }

        //check that we have enough money
        foreach (var currency in msg.Listing.Cost)
            if (!component.Balance.TryGetValue(currency.Key, out var balance) || balance < currency.Value)
                return;

        foreach (var currency in msg.Listing.Cost)
            component.Balance[currency.Key] -= currency.Value;

        if (msg.Listing.ProductEntity != null)
        {
            var product = Spawn(msg.Listing.ProductEntity, Transform(msg.Buyer).Coordinates);
            _hands.TryPickupAnyHand(msg.Buyer, product);
        }

        if (msg.Listing.ProductAction != null)
        {
            var action = new InstantAction(_proto.Index<InstantActionPrototype>(msg.Listing.ProductAction));
            _actions.AddAction(msg.Buyer, action, null);
        }

        if (msg.Listing.ProductEvent != null)
        {
            RaiseLocalEvent(msg.Listing.ProductEvent);
        }

        if (TryComp<MindComponent>(msg.Buyer, out var mind))
        {
            _admin.Add(LogType.StorePurchase, LogImpact.Medium,
                $"{ToPrettyString(mind.Owner):player} purchased listing \"{msg.Listing.Name}\" from {ToPrettyString(uid)}");
        }

        foreach (var listing in component.Listings)
        {
            var b = listing == msg.Listing;
            Logger.Debug(b.ToString());
        }
    }
}
