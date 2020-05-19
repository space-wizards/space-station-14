using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.PDA
{
    [Prototype("uplinkListing")]
    public class UplinkStoreListingPrototype : IPrototype, IIndexedPrototype
    {

        private string _id;
        private string _itemID;
        private int _price;
        private string _category;
        private string _desc;
        private string _name;

        public string ID => _id;

        public string ItemID => _itemID;
        public int Price => _price;
        public string Category => _category;
        public string Description => _desc;
        public string ListingName => _name;
        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _itemID, "itemId", string.Empty);
            serializer.DataField(ref _price, "price", 5);
            serializer.DataField(ref _category, "category", string.Empty);
            serializer.DataField(ref _desc, "description", string.Empty);
            serializer.DataField(ref _name, "listingName", string.Empty);

        }


    }
}
