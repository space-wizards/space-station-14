using Content.Shared.PDA;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public class UplinkListingData : ComponentState, IEquatable<UplinkListingData>
    {
        public string ItemId;
        public int Price;
        public UplinkCategory Category;
        public string Description;
        public string ListingName;

        public UplinkListingData(string listingName, string itemId,
            int price, UplinkCategory category,
            string description)
        {
            ListingName = listingName;
            Price = price;
            Category = category;
            Description = description;
            ItemId = itemId;
        }

        public bool Equals(UplinkListingData? other)
        {
            if (other == null)
            {
                return false;
            }

            return ItemId == other.ItemId;
        }
    }
}
