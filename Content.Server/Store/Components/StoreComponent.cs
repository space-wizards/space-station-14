using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Store.Components;

[RegisterComponent]
public sealed class StoreComponent : Component
{
    /// <summary>
    /// All the listing categories that are available on this store.
    /// The available listings are partially based on the categories.
    /// </summary>
    [DataField("categories", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<StoreCategoryPrototype>))]
    public HashSet<string> Categories = new() { "Debug", "Debug2" };

    /// <summary>
    /// The total amount of currency that can be used in the store.
    /// The string represents the ID of te currency prototype, where the
    /// float is that amount.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("currency", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, CurrencyPrototype>))]
    public Dictionary<string, float> Currency = new();

    /// <summary>
    /// Whether or not this store can be activated by clicking on it (like an uplink)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activateInHand")]
    public bool ActivateInHand = true;

    /// <summary>
    /// The person who "owns" the store/account. Used if you want the listings to be fixed
    /// regardless of who activated it. I.E. role specific items for uplinks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AccountOwner = null;

    public HashSet<ListingData> Listings = new();
}
