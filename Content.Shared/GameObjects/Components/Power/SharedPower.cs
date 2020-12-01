using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum ChargeState : byte
    {
        Still,
        Charging,
        Discharging,
    }
}
