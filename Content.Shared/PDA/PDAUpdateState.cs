using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;


namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public sealed class PDAUpdateState : BoundUserInterfaceState
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
    public struct PDAIdInfoText
    {
        public string? ActualOwnerName;
        public string? IdOwner;
        public string? JobTitle;
    }
}
