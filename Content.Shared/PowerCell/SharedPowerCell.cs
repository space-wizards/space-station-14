#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.PowerCell
{
    public static class SharedPowerCell
    {
        public const int PowerCellVisualsLevels = 4;
    }

    [Serializable, NetSerializable]
    public enum PowerCellVisuals
    {
        ChargeLevel
    }
}
