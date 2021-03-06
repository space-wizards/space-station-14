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
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [field: DataField("itemId")]
        public string ItemId { get; } = string.Empty;

        [field: DataField("price")]
        public int Price { get; } = 5;

        [field: DataField("category")]
        public UplinkCategory Category { get; } = UplinkCategory.Utility;

        [field: DataField("description")]
        public string Description { get; } = string.Empty;

        [field: DataField("listingName")]
        public string ListingName { get; } = string.Empty;
    }
}
