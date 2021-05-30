#nullable enable
using Content.Shared.GameObjects.Components.PDA;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Prototypes.PDA
{
    [Prototype("uplinkListing")]
    public class UplinkStoreListingPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("itemId")]
        public string ItemId { get; } = string.Empty;

        [DataField("price")]
        public int Price { get; } = 5;

        [DataField("category")]
        public UplinkCategory Category { get; } = UplinkCategory.Utility;

        [DataField("description")]
        public string Description { get; } = string.Empty;

        [DataField("listingName")]
        public string ListingName { get; } = string.Empty;
    }
}
