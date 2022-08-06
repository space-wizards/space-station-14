using Robust.Shared.Serialization;


namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public sealed class PDAUpdateState : BoundUserInterfaceState
    {
        public bool FlashlightEnabled;
        public bool HasPen;
        public PDAIdInfoText PDAOwnerInfo;
        public string? StationName;
        public bool HasUplink;
        public bool CanPlayMusic;

        public PDAUpdateState(bool flashlightEnabled, bool hasPen, PDAIdInfoText pDAOwnerInfo, string? stationName, bool hasUplink = false, bool canPlayMusic = false)
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PDAOwnerInfo = pDAOwnerInfo;
            HasUplink = hasUplink;
            CanPlayMusic = canPlayMusic;
            StationName = stationName;
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
