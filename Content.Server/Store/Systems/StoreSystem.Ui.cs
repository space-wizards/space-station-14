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
using Content.Server.Stack;
using Content.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    private void InitializeUi()
    {
        SubscribeLocalEvent<StoreComponent, StoreRequestUpdateInterfaceMessage>((_,c,r) => UpdateUserInterface(r.CurrentBuyer, c));
        SubscribeLocalEvent<StoreComponent, StoreBuyListingMessage>(OnBuyRequest);
        SubscribeLocalEvent<StoreComponent, StoreRequestWithdrawMessage>(OnRequestWithdraw);
    }

    /// <summary>
    /// Toggles the store Ui open and closed
    /// </summary>
    /// <param name="user">the person doing the toggling</param>
    /// <param name="component">the store being toggled</param>
    public void ToggleUi(EntityUid user, StoreComponent component)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        var ui = component.Owner.GetUIOrNull(StoreUiKey.Key);
        ui?.Toggle(actor.PlayerSession);

        UpdateUserInterface(user, component, ui);
    }

    /// <summary>
    /// Updates the user interface for a store and refreshes the listings
    /// </summary>
    /// <param name="user">The person who if opening the store ui. Listings are filtered based on this.</param>
    /// <param name="component">The store component being refreshed.</param>
    /// <param name="ui"></param>
    public void UpdateUserInterface(EntityUid? user, StoreComponent component, BoundUserInterface? ui = null)
    {
        if (ui == null)
        {
            ui = component.Owner.GetUIOrNull(StoreUiKey.Key);
            if (ui == null)
                return;
        }

        //if we haven't opened it before, initialize the shit
        if (!component.Opened)
        {
            RefreshAllListings(component);
            InitializeFromPreset(component.Preset, component);
            component.Opened = true;
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

    /// <summary>
    /// Handles whenever a purchase was made.
    /// </summary>
    private void OnBuyRequest(EntityUid uid, StoreComponent component, StoreBuyListingMessage msg)
    {
        ListingData? listing = component.Listings.FirstOrDefault(x => x.Equals(msg.Listing));
        if (listing == null) //make sure this listing actually exists
        {
            Logger.Debug("listing does not exist");
            return;
        }

        //verify that we can actually buy this listing and it wasn't added
        if (!ListingHasCategory(listing, component.Categories))
            return;
        //condition checking because why not
        if (listing.Conditions != null)
        {
            var args = new ListingConditionArgs(msg.Buyer, component.Owner, listing, EntityManager);
            var conditionsMet = true;

            foreach (var condition in listing.Conditions.Where(condition => !condition.Condition(args)))
                conditionsMet = false;

            if (!conditionsMet)
                return;
        }

        //check that we have enough money
        foreach (var currency in listing.Cost)
        {
            if (!component.Balance.TryGetValue(currency.Key, out var balance) || balance < currency.Value)
            {
                _audio.Play(component.InsufficientFundsSound, Filter.SinglePlayer(msg.Session), uid);
                return;
            }
        }
        //subtract the cash
        foreach (var currency in listing.Cost)
            component.Balance[currency.Key] -= currency.Value;

        //spawn entity
        if (listing.ProductEntity != null)
        {
            var product = Spawn(listing.ProductEntity, Transform(msg.Buyer).Coordinates);
            _hands.TryPickupAnyHand(msg.Buyer, product);
        }

        //give action
        if (listing.ProductAction != null)
        {
            var action = new InstantAction(_proto.Index<InstantActionPrototype>(listing.ProductAction));
            _actions.AddAction(msg.Buyer, action, null);
        }

        //broadcast event
        if (listing.ProductEvent != null)
        {
            RaiseLocalEvent(listing.ProductEvent);
        }

        //log dat shit.
        if (TryComp<MindComponent>(msg.Buyer, out var mind))
        {
            _admin.Add(LogType.StorePurchase, LogImpact.Low,
                $"{ToPrettyString(mind.Owner):player} purchased listing \"{listing.Name}\" from {ToPrettyString(uid)}");
        }

        listing.PurchaseAmount++; //track how many times something has been purchased
        _audio.Play(component.BuySuccessSound, Filter.SinglePlayer(msg.Session), uid); //cha-ching!

        UpdateUserInterface(msg.Buyer, component);
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
        //make sure we have enough cash in the bank and we actually support this currency
        if (!component.Balance.TryGetValue(msg.Currency, out var currentAmount) || currentAmount < msg.Amount)
            return;

        //make sure a malicious client didn't send us random shit
        if (!_proto.TryIndex<CurrencyPrototype>(msg.Currency, out var proto))
            return;

        //we need an actually valid entity to spawn. This check has been done earlier, but just in case.
        if (proto.EntityId == null || !proto.CanWithdraw)
            return;

        var entproto = _proto.Index<EntityPrototype>(proto.EntityId);

        var amountRemaining = msg.Amount;
        var coordinates = Transform(msg.Buyer).Coordinates;
        if (entproto.HasComponent<StackComponent>())
        {
            while (amountRemaining > 0)
            {
                var ent = Spawn(proto.EntityId, coordinates);
                var stackComponent = Comp<StackComponent>(ent); //we already know it exists

                var amountPerStack = Math.Min(stackComponent.MaxCount, amountRemaining);

                _stack.SetCount(ent, amountPerStack, stackComponent);
                amountRemaining -= amountPerStack;
                _hands.TryPickupAnyHand(msg.Buyer, ent);
            }
        }
        else //please for the love of christ give your currency stack component
        {
            while (amountRemaining > 0)
            {
                var ent = Spawn(proto.EntityId, coordinates);
                _hands.TryPickupAnyHand(msg.Buyer, ent);
                amountRemaining--;
            }
        }

        component.Balance[msg.Currency] -= msg.Amount;
        UpdateUserInterface(msg.Buyer, component);
    }
}
