using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Component
{
    [NetworkedComponent]
    public abstract class SharedHandheldLightComponent : Robust.Shared.GameObjects.Component
    {
        public const int StatusLevels = 6;

        [Serializable, NetSerializable]
        public sealed class HandheldLightComponentState : ComponentState
        {
            public byte? Charge { get; }

            public HandheldLightComponentState(byte? charge)
            {
                Charge = charge;
            }
        }
    }

    [Serializable, NetSerializable]
    public enum HandheldLightVisuals
    {
        Power
    }

    [Serializable, NetSerializable]
    public enum HandheldLightPowerStates
    {
        FullPower,
        LowPower,
        Dying,
    }


}
