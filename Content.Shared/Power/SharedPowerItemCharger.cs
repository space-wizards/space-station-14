using Robust.Shared.Serialization;

namespace Content.Shared.Power
{
    [Serializable, NetSerializable]
    public enum CellChargerStatus
    {
        Off,
        Empty,
        Charging,
        Charged,
    }

    [Serializable, NetSerializable]
    public enum CellVisual
    {
        Occupied, // If there's an item in it
        Light,
    }
}
