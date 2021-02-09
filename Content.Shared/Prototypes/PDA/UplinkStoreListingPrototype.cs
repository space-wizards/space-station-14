using Content.Shared.GameObjects.Components.PDA;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.PDA
{
    [Prototype("uplinkListing")]
    public class UplinkStoreListingPrototype : IPrototype, IIndexedPrototype
    {

        [YamlField("id")]
        private string _id;
        [YamlField("itemId")]
        private string _itemId;
        [YamlField("price")]
        private int _price = 5;
        [YamlField("category")]
        private UplinkCategory _category = UplinkCategory.Utility;
        [YamlField("description")]
        private string _desc;
        [YamlField("listingName")]
        private string _name;

        public string ID => _id;

        public string ItemId => _itemId;
        public int Price => _price;
        public UplinkCategory Category => _category;
        public string Description => _desc;
        public string ListingName => _name;
    }
}
