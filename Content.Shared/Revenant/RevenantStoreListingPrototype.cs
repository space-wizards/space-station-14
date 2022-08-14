using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Revenant;

[Serializable]
[Prototype("revenantListing")]
public sealed class RevenantStoreListingPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("actionId", customTypeSerializer:typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string ActionId { get; } = string.Empty;

    [DataField("price")]
    public int Price { get; } = 5;

    [DataField("description")]
    public string Description { get; } = string.Empty;

    [DataField("listingName")]
    public string ListingName { get; } = string.Empty;

    [DataField("icon")]
    public SpriteSpecifier? Icon { get; } = null;
}
