using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.GPS
{
    [Serializable, NetSerializable]
    public sealed class UpdateGPSLocationState : BoundUserInterfaceState
    {
        public MapCoordinates? Coordinates;
        public UpdateGPSLocationState(MapCoordinates? coordinates)
        {
            Coordinates = coordinates;
        }
    }

    [Serializable, NetSerializable]
    public enum GPSUiKey
    {
        Key
    }
}
