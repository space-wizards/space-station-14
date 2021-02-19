#nullable enable
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    [Serializable, NetSerializable]
    public enum PoweredLightVisuals
    {
        BulbState,
        BulbColor,
        Blinking
    }

    [Serializable, NetSerializable]
    public enum PoweredLightState
    {
        Empty,
        On,
        Off,
        Broken,
        Burned
    }
}
