using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;


namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public sealed class PDAUpdateState : CartridgeLoaderUiState
    {
        public bool FlashlightEnabled;
        public bool HasPen;
        public PdaIdInfoText PdaOwnerInfo;
        public StationTimeText StationTime;
        public StationAlert StationAlert;
        public List<string> AccessLevels;
        public string? StationName;
        public bool HasUplink;
        public bool CanPlayMusic;
        public string? Address;

        public PDAUpdateState(bool flashlightEnabled, bool hasPen, PdaIdInfoText pdaOwnerInfo, List<string> accessLevels,
            StationTimeText stationTime, StationAlert stationAlert, string? stationName, bool hasUplink = false,
            bool canPlayMusic = false, string? address = null)
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PdaOwnerInfo = pdaOwnerInfo;
            HasUplink = hasUplink;
            CanPlayMusic = canPlayMusic;
            StationName = stationName;
            Address = address;
            StationTime = stationTime;
            AccessLevels = accessLevels;
            StationAlert = stationAlert;
        }
    }

    [Serializable, NetSerializable]
    public struct PdaIdInfoText
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

    [Serializable, NetSerializable]
    public struct StationAlert
    {
        public string? Level;
        public Color Color;
    }
}