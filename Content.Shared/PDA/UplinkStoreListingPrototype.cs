using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.PDA
{
    [Prototype("uplinkListing")]
    public class UplinkStoreListingPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("itemId", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemId { get; } = string.Empty;

        [DataField("price")]
        public int Price { get; } = 5;

        [DataField("category")]
        public UplinkCategory Category { get; } = UplinkCategory.Utility;

        [DataField("description")]
        public string Description { get; } = string.Empty;

        [DataField("listingName")]
        public string ListingName { get; } = string.Empty;

        [DataField("icon")]
        public SpriteSpecifier? Icon { get; } = null;
    }
}
