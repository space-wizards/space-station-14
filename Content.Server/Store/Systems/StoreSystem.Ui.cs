using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Mind.Components;
using Content.Server.Store.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Store;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using System.Linq;
using Content.Server.Stack;
using Robust.Shared.Player;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void InitializeUi()
    {
        SubscribeLocalEvent<StoreComponent, StoreRequestUpdateInterfaceMessage>((_,c,r) => UpdateUserInterface(r.Session.AttachedEntity, c));
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

        if (!_ui.TryToggleUi(component.Owner, StoreUiKey.Key, actor.PlayerSession))
            return;

        UpdateUserInterface(user, component);
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
            ui = _ui.GetUiOrNull(component.Owner, StoreUiKey.Key);
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
        if (user != null) //if we have no "buyer" for this update, then don't update the listings
        {
            component.LastAvailableListings = GetAvailableListings(component.AccountOwner ?? user.Value, component).ToHashSet();
        }

        //dictionary for all currencies, including 0 values for currencies on the whitelist
        Dictionary<string, FixedPoint2> allCurrency = new();
        foreach (var supported in component.CurrencyWhitelist)
        {
            allCurrency.Add(supported, FixedPoint2.Zero);

            if (component.Balance.ContainsKey(supported))
                allCurrency[supported] = component.Balance[supported];
        }

        // TODO: if multiple users are supposed to be able to interact with a single BUI & see different
        // stores/listings, this needs to use session specific BUI states.

        var state = new StoreUpdateState(component.LastAvailableListings, allCurrency);
        _ui.SetUiState(ui, state);
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

        if (msg.Session.AttachedEntity is not { Valid: true } buyer)
            return;

        //verify that we can actually buy this listing and it wasn't added
        if (!ListingHasCategory(listing, component.Categories))
            return;

        //condition checking because why not
        if (listing.Conditions != null)
        {
            var args = new ListingConditionArgs(component.AccountOwner ?? buyer, component.Owner, listing, EntityManager);
            var conditionsMet = listing.Conditions.All(condition => condition.Condition(args));

            if (!conditionsMet)
                return;
        }

        //check that we have enough money
        foreach (var currency in listing.Cost)
        {
            if (!component.Balance.TryGetValue(currency.Key, out var balance) || balance < currency.Value)
            {
                return;
            }
        }
        //subtract the cash
        foreach (var currency in listing.Cost)
        {
            component.Balance[currency.Key] -= currency.Value;
        }

        //spawn entity
        if (listing.ProductEntity != null)
        {
            var product = Spawn(listing.ProductEntity, Transform(buyer).Coordinates);
            _hands.PickupOrDrop(buyer, product);
        }

        //give action
        if (listing.ProductAction != null)
        {
            var action = new InstantAction(_proto.Index<InstantActionPrototype>(listing.ProductAction));
            _actions.AddAction(buyer, action, null);
        }

        //broadcast event
        if (listing.ProductEvent != null)
        {
            RaiseLocalEvent(listing.ProductEvent);
        }

        //log dat shit.
        if (TryComp<MindComponent>(buyer, out var mind))
        {
            _admin.Add(LogType.StorePurchase, LogImpact.Low,
                $"{ToPrettyString(mind.Owner):player} purchased listing \"{listing.Name}\" from {ToPrettyString(uid)}");
        }

        listing.PurchaseAmount++; //track how many times something has been purchased
        _audio.PlayEntity(component.BuySuccessSound, msg.Session, uid); //cha-ching!

        UpdateUserInterface(buyer, component);
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
        if (proto.Cash == null || !proto.CanWithdraw)
            return;

        if (msg.Session.AttachedEntity is not { Valid: true} buyer)
            return;

        FixedPoint2 amountRemaining = msg.Amount;
        var coordinates = Transform(buyer).Coordinates;

        var sortedCashValues = proto.Cash.Keys.OrderByDescending(x => x).ToList();
        foreach (var value in sortedCashValues)
        {
            var cashId = proto.Cash[value];
            var amountToSpawn = (int) MathF.Floor((float) (amountRemaining / value));
            var ents = _stack.SpawnMultipleFromMaterial(amountToSpawn, cashId, coordinates);
            _hands.PickupOrDrop(buyer, ents.First());
            amountRemaining -= value * amountToSpawn;
        }

        component.Balance[msg.Currency] -= msg.Amount;
        UpdateUserInterface(buyer, component);
    }
}
