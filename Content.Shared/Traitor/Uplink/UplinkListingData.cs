using Content.Shared.PDA;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public class UplinkListingData : ComponentState, IEquatable<UplinkListingData>
    {
        public readonly string ItemId;
        public readonly int Price;
        public readonly UplinkCategory Category;
        public readonly string Description;
        public readonly string ListingName;
        public readonly SpriteSpecifier? Icon;

        public UplinkListingData(string listingName, string itemId,
            int price, UplinkCategory category,
            string description, SpriteSpecifier? icon)
        {
            ListingName = listingName;
            Price = price;
            Category = category;
            Description = description;
            ItemId = itemId;
            Icon = icon;
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
