using Content.Shared.GameObjects.Components.PDA;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.PDA
{
    [Prototype("uplinkListing")]
    public class UplinkStoreListingPrototype : IPrototype, IIndexedPrototype
    {

        [DataField("id")]
        private string _id;
        [DataField("itemId")]
        private string _itemId;
        [DataField("price")]
        private int _price = 5;
        [DataField("category")]
        private UplinkCategory _category = UplinkCategory.Utility;
        [DataField("description")]
        private string _desc;
        [DataField("listingName")]
        private string _name;

        public string ID => _id;

        public string ItemId => _itemId;
        public int Price => _price;
        public UplinkCategory Category => _category;
        public string Description => _desc;
        public string ListingName => _name;
    }
}
