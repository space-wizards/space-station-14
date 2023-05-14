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
        public StationAlert StationAlert;
        public string? StationName;
        public bool HasUplink;
        public bool CanPlayMusic;
        public string? Address;

        public PDAUpdateState(bool flashlightEnabled, bool hasPen, PDAIdInfoText pdaOwnerInfo,
            StationAlert stationAlert, string? stationName, bool hasUplink = false,
            bool canPlayMusic = false, string? address = null)
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            PDAOwnerInfo = pdaOwnerInfo;
            HasUplink = hasUplink;
            CanPlayMusic = canPlayMusic;
            StationName = stationName;
            Address = address;
            StationAlert = stationAlert;
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
    public struct StationAlert
    {
        public string? Level;
        public Color Color;
    }
}
