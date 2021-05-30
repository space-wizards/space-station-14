#nullable enable
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    [Serializable, NetSerializable]
    public enum PoweredLightVisuals : byte
    {
        BulbState,
        BulbColor,
        Blinking
    }

    [Serializable, NetSerializable]
    public enum PoweredLightState : byte
    {
        Empty,
        On,
        Off,
        Broken,
        Burned
    }

    public enum PoweredLightLayers : byte
    {
        Base
    }
}
