#nullable enable
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Watercloset
{
    [Serializable, NetSerializable]
    public enum ToiletVisuals
    {
        LidOpen,
        SeatUp
    }
}
