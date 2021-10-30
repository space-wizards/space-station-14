using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Power
{
    [Serializable, NetSerializable]
    public enum ChargeState
    {
        Still,
        Charging,
        Discharging,
    }
}
