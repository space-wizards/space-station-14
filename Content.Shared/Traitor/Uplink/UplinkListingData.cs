using Content.Shared.PDA;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public sealed class UplinkListingData : ComponentState, IEquatable<UplinkListingData>
    {
        public readonly string ItemId;
        public readonly int Price;
        public readonly UplinkCategory Category;
        public readonly string Description;
        public readonly string ListingName;
        public readonly SpriteSpecifier? Icon;
        public readonly HashSet<string>? JobWhitelist;

        public UplinkListingData(string listingName, string itemId,
            int price, UplinkCategory category,
            string description, SpriteSpecifier? icon, HashSet<string>? jobWhitelist)
        {
            ListingName = listingName;
            Price = price;
            Category = category;
            Description = description;
            ItemId = itemId;
            Icon = icon;
            JobWhitelist = jobWhitelist;
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
