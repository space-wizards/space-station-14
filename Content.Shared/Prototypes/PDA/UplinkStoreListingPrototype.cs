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

        [ViewVariables]
        [field: DataField("parent")]
        public string Parent { get; }

        [field: DataField("itemId")]
        public string ItemId { get; }

        [field: DataField("price")]
        public int Price { get; } = 5;

        [field: DataField("category")]
        public UplinkCategory Category { get; } = UplinkCategory.Utility;

        [field: DataField("description")]
        public string Description { get; }

        [field: DataField("listingName")]
        public string ListingName { get; }
    }
}
