using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;


namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public sealed class PDAUpdateState : CartridgeLoaderUiState
    {
        public bool FlashlightEnabled;
        public bool HasPen;
        public PDAIdInfoText PDAOwnerInfo;
        public StationTimeText StationTime;
        public List<string> AccessLevels;
        public string? StationName;
        public string? StationAlertLevel;
        public Color StationAlertColor;
        public bool HasUplink;
        public bool CanPlayMusic;
        public string? Address;

        public PDAUpdateState(bool flashlightEnabled, bool hasPen, PDAIdInfoText pdaOwnerInfo, List<string> accessLevels,
            StationTimeText stationTime, string? stationName, bool hasUplink = false, bool canPlayMusic = false,
            string? address = null, string? stationAlertLevel = null, Color stationAlertColor = default)
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PDAOwnerInfo = pdaOwnerInfo;
            HasUplink = hasUplink;
            CanPlayMusic = canPlayMusic;
            StationName = stationName;
            Address = address;
            StationAlertLevel = stationAlertLevel;
            StationTime = stationTime;
            AccessLevels = accessLevels;
            StationAlertColor = stationAlertColor;
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
    public struct StationTimeText
    {
        public string? Hours;
        public string? Minutes;
    }
}
