using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.PDA
{
    public class SharedPDAComponent : Component
    {
        public override string Name => "PDA";
        public override uint? NetID => ContentNetIDs.PDA;

        public override void Initialize()
        {
            base.Initialize();
        }


    }

    [Serializable, NetSerializable]
    public sealed class PDAToggleFlashlightMessage : BoundUserInterfaceMessage
    {
        public PDAToggleFlashlightMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAEjectIDMessage : BoundUserInterfaceMessage
    {
        public PDAEjectIDMessage()
        {

        }
    }


    [Serializable, NetSerializable]
    public class PDAUBoundUserInterfaceState : BoundUserInterfaceState
    {

    }

    [Serializable, NetSerializable]
    public sealed class PDAUpdateMainMenuState : PDAUBoundUserInterfaceState
    {
        public bool FlashlightEnabled;
        public PDAIdInfoText PDAOwnerInfo;


        public PDAUpdateMainMenuState(bool isFlashlightOn, PDAIdInfoText ownerInfo)
        {
            FlashlightEnabled = isFlashlightOn;
            PDAOwnerInfo = ownerInfo;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PDASendUplinkListingsMessage : PDAUBoundUserInterfaceState
    {

        public UplinkListingData[] Listings;
        public PDASendUplinkListingsMessage(UplinkListingData[] listings)
        {
            Listings = listings;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PDARequestUplinkListingsMessage : BoundUserInterfaceMessage
    {
        public PDARequestUplinkListingsMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAUplinkBuyListingMessage : BoundUserInterfaceMessage
    {
        public UplinkListingData ListingToBuy;
        public PDAUplinkBuyListingMessage(UplinkListingData itemToBuy)
        {
            ListingToBuy = itemToBuy;
        }
    }


    [NetSerializable, Serializable]
    public struct PDAIdInfoText
    {
        public string ActualOwnerName;
        public string IDOwner;
        public string JobTitle;
    }

    [NetSerializable, Serializable]
    public enum PDAVisuals
    {
        ScreenLit,
    }

    [NetSerializable, Serializable]
    public enum PDAUiKey
    {
        Key
    }

    [NetSerializable, Serializable]
    public class UplinkAccount
    {
        public EntityUid AccountHolder;
        public int Balance;

        public UplinkAccount(EntityUid uid, int startingBalance)
        {
            AccountHolder = uid;
            Balance = startingBalance;
        }
    }

    [NetSerializable, Serializable]
    public class UplinkListingData : ComponentState, IEquatable<UplinkListingData>
    {
        public string ItemID;
        public int Price;
        public string Category;
        public string Description;
        public string ListingName;

        public UplinkListingData(string listingName,string itemId,
            int price, string category, string description) : base(ContentNetIDs.PDA)
        {
            ListingName = listingName;
            Price = price;
            Category = category;
            Description = description;
            ItemID = itemId;
        }

        public bool Equals(UplinkListingData other)
        {
            if (other == null)
            {
                return false; //eat shit
            }

            return this.ItemID == other.ItemID;
        }
    }

}
