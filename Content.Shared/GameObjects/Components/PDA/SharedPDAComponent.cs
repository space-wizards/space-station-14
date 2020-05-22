using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.PDA
{
    public class SharedPDAComponent : Component
    {
        public override string Name => "PDA";
        public override uint? NetID => ContentNetIDs.PDA;

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
    public sealed class PDAUpdateUplinkAccountMessage : PDAUBoundUserInterfaceState
    {

        public UplinkAccountData Account;
        public PDAUpdateUplinkAccountMessage(UplinkAccountData userAccount)
        {
            Account = userAccount;
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

    [Serializable, NetSerializable]
    public sealed class PDARequestUpdateInterfaceMessage : BoundUserInterfaceMessage
    {
        public PDARequestUpdateInterfaceMessage()
        {

        }
    }


    [NetSerializable, Serializable]
    public struct PDAIdInfoText
    {
        public string ActualOwnerName;
        public string IdOwner;
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

    public class UplinkAccount
    {
        public event Action<UplinkAccount> BalanceChanged;
        public EntityUid AccountHolder;
        public int Balance { get; private set; }

        public UplinkAccount(EntityUid uid, int startingBalance)
        {
            AccountHolder = uid;
            Balance = startingBalance;
        }

        public bool ModifyAccountBalance(int newBalance)
        {
            if (newBalance < 0)
            {
                return false;
            }
            Balance = newBalance;
            BalanceChanged?.Invoke(this);
            return true;

        }
    }

    [NetSerializable, Serializable]
    public class UplinkAccountData
    {
        public EntityUid DataAccountHolder;
        public int DataBalance;

        public UplinkAccountData(EntityUid dataAccountHolder, int dataBalance)
        {
            DataAccountHolder = dataAccountHolder;
            DataBalance = dataBalance;
        }
    }

    [NetSerializable, Serializable]
    public class UplinkListingData : ComponentState, IEquatable<UplinkListingData>
    {
        public string ItemId;
        public int Price;
        public UplinkCategory Category;
        public string Description;
        public string ListingName;
        public Color DisplayColor;

        public UplinkListingData(string listingName,string itemId,
            int price, UplinkCategory category,
            string description, Color displayColor) : base(ContentNetIDs.PDA)
        {
            ListingName = listingName;
            Price = price;
            Category = category;
            Description = description;
            ItemId = itemId;
            DisplayColor = displayColor;
        }

        public bool Equals(UplinkListingData other)
        {
            if (other == null)
            {
                return false;
            }

            return ItemId == other.ItemId;
        }
    }

}
