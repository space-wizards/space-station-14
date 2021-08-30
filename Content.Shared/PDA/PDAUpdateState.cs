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
        public bool Uplink;

        public PDAUpdateState(bool flashlightEnabled, bool hasPen, PDAIdInfoText pDAOwnerInfo, bool uplink = false)
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PDAOwnerInfo = pDAOwnerInfo;
            Uplink = uplink;
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
