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
        public bool HasUplink;

        public PDAUpdateState(bool flashlightEnabled, bool hasPen, PDAIdInfoText pDAOwnerInfo, bool hasUplink = false)
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PDAOwnerInfo = pDAOwnerInfo;
            HasUplink = hasUplink;
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
