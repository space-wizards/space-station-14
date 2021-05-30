#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum SmesVisuals
    {
        LastChargeState,
        LastChargeLevel,
    }
}
