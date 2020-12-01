using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum CellChargerStatus : byte
    {
        Off,
        Empty,
        Charging,
        Charged,
    }

    [Serializable, NetSerializable]
    public enum CellVisual : byte
    {
        Occupied, // If there's an item in it
        Light,
    }
}
