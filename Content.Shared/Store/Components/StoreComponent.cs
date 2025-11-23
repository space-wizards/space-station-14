using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store.Components;

/// <summary>
/// This component manages a store which players can use to purchase different listings
/// through the ui. The currency, listings, and categories are defined in yaml.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class StoreComponent : Component
{
    [DataField]
    public LocId Name = "store-ui-default-title";

    /// <summary>
    /// All the listing categories that are available on this store.
    /// The available listings are partially based on the categories.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<StoreCategoryPrototype>> Categories = new();

    /// <summary>
    /// The total amount of currency that can be used in the store.
    /// The string represents the ID of te currency prototype, where the
    /// float is that amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Balance = new();

    /// <summary>
    /// The list of currencies that can be inserted into this store.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CurrencyPrototype>> CurrencyWhitelist = new();

    /// <summary>
    /// The person/mind who "owns" the store/account. Used if you want the listings to be fixed
    /// regardless of who activated it. I.E. role specific items for uplinks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AccountOwner;

    /// <summary>
    /// Contains all modified listings for some default listings.
    /// When we try to get a listing with an ID that is contained here,
    /// we take the value from the dictionary instead of indexing the prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ListingDataWithCostModifiers> ListingsModifiers = new();

    /// <summary>
    ///     All current entities bought from this shop. Useful for keeping track of refunds and upgrades.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> BoughtEntities = new();

    /// <summary>
    ///     The total balance spent in this store. Used for refunds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> BalanceSpent = new();

    /// <summary>
    ///     Controls if the store allows refunds
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RefundAllowed;

    /// <summary>
    ///     Checks if store can be opened by the account owner only.
    ///     Not meant to be used with uplinks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OwnerOnly;

    /// <summary>
    ///     The map the store was originally from, used to block refunds if the map is changed
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? StartingMap;

    /// <summary>
    /// The sound played to the buyer when a purchase is succesfully made.
    /// </summary>
    [DataField]
    public SoundSpecifier? BuySuccessSound = new SoundPathSpecifier("/Audio/Effects/kaching.ogg");
}

/// <summary>
/// Event that is broadcast when a store is added to an entity
/// </summary>
[ByRefEvent]
public readonly record struct StoreAddedEvent;
/// <summary>
/// Event that is broadcast when a store is removed from an entity
/// </summary>
[ByRefEvent]
public readonly record struct StoreRemovedEvent;

/// <summary>
///     Broadcast when an Entity with the <see cref="StoreRefundComponent"/> is deleted
/// </summary>
[ByRefEvent]
public readonly struct RefundEntityDeletedEvent
{
    public EntityUid Uid { get; }

    public RefundEntityDeletedEvent(EntityUid uid)
    {
        Uid = uid;
    }
}
