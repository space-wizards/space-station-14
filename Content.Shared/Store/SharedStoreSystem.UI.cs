using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedStoreSystem
{
    /// <summary>
    /// Toggles the store Ui open and closed
    /// </summary>
    /// <param name="user">the person doing the toggling</param>
    /// <param name="storeEnt">the store being toggled</param>
    /// <param name="component"></param>
    /// <param name="remoteAccess">The entity remotely accessing the store, if any.</param>
    /// <param name="remoteComponent">The remote access component, if any.</param>
    public void ToggleUi(EntityUid user, EntityUid storeEnt, StoreComponent? component = null, EntityUid? remoteAccess = null, RemoteStoreComponent? remoteComponent = null)
    {
        if (!Resolve(storeEnt, ref component))
            return;

        if (remoteAccess != null && !Resolve(remoteAccess.Value, ref remoteComponent) && remoteComponent!.Store != storeEnt)
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!UI.TryToggleUi(remoteAccess != null ? remoteAccess.Value : storeEnt, StoreUiKey.Key, actor.PlayerSession))
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

        UI.CloseUi(uid, StoreUiKey.Key);
    }

    /// <summary>
    /// Updates the user interface for a store and refreshes the listings
    /// </summary>
    /// <param name="user">The person who if opening the store ui. Listings are filtered based on this.</param>
    /// <param name="store">The store entity itself</param>
    /// <param name="component">The store component being refreshed.</param>
    public void UpdateUserInterface(EntityUid? user, EntityUid store, StoreComponent? component = null)
    {
        if (!Resolve(store, ref component))
            return;

        //this is the person who will be passed into logic for all listing filtering.
        if (user != null) //if we have no "buyer" for this update, then don't update the listings
        {
            component.LastAvailableListings = GetAvailableListings(component.AccountOwner ?? user.Value, store, component).ToHashSet();
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
        UpdateRemoteStores(store, state);
        UI.SetUiState(store, StoreUiKey.Key, state);
    }

    /// <summary>
    /// Updates any remote store connections to a specific store.
    /// </summary>
    /// <param name="store">The store being updated.</param>
    /// <param name="state">The state being applied.</param>
    public void UpdateRemoteStores(EntityUid store, StoreUpdateState state)
    {
        var query = EntityQueryEnumerator<RemoteStoreComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var remote, out var ui))
        {
            if (remote.Store != store)
                continue;

            UI.SetUiState((uid, ui), StoreUiKey.Key, state);
        }
    }
}
