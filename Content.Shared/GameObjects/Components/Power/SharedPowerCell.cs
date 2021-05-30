#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
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
