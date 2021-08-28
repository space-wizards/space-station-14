using System;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.PDA
{
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
    public sealed class PDAEjectPenMessage : BoundUserInterfaceMessage
    {
        public PDAEjectPenMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public class PDAUBoundUserInterfaceState : BoundUserInterfaceState
    {

    }

    [Serializable, NetSerializable]
    public sealed class PDAUpdateState : PDAUBoundUserInterfaceState
    {
        public bool FlashlightEnabled;
        public bool HasPen;
        public PDAIdInfoText PDAOwnerInfo;
        public UplinkAccountData Account = default!;
        public UplinkListingData[] Listings = default!;

        public PDAUpdateState(bool isFlashlightOn, bool hasPen, PDAIdInfoText ownerInfo)
        {
            FlashlightEnabled = isFlashlightOn;
            HasPen = hasPen;
            PDAOwnerInfo = ownerInfo;
        }

        public PDAUpdateState(bool isFlashlightOn, bool hasPen, PDAIdInfoText ownerInfo, UplinkAccountData accountData)
            : this(isFlashlightOn, hasPen, ownerInfo)
        {
            Account = accountData;
        }

        public PDAUpdateState(bool isFlashlightOn, bool hasPen, PDAIdInfoText ownerInfo, UplinkAccountData accountData, UplinkListingData[] listings)
            : this(isFlashlightOn, hasPen, ownerInfo, accountData)
        {
            Listings = listings;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAUplinkBuyListingMessage : BoundUserInterfaceMessage
    {
        public string ItemId;

        public PDAUplinkBuyListingMessage(string itemId)
        {
            ItemId = itemId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PDARequestUpdateInterfaceMessage : BoundUserInterfaceMessage
    {
        public PDARequestUpdateInterfaceMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public struct PDAIdInfoText
    {
        public string? ActualOwnerName;
        public string? IdOwner;
        public string? JobTitle;
    }

    [Serializable, NetSerializable]
    public enum PDAVisuals
    {
        FlashlightLit,
        IDCardInserted
    }

    [Serializable, NetSerializable]
    public enum PDAUiKey
    {
        Key
    }

}
