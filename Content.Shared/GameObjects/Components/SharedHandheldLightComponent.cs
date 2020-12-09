using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedHandheldLightComponent : Component
    {
        public sealed override string Name => "HandheldLight";
        public sealed override uint? NetID => ContentNetIDs.HANDHELD_LIGHT;

        protected abstract bool HasCell { get; }

        protected const int StatusLevels = 6;

        [Serializable, NetSerializable]
        protected sealed class HandheldLightComponentState : ComponentState
        {
            public byte? Charge { get; }

            public HandheldLightComponentState(byte? charge) : base(ContentNetIDs.HANDHELD_LIGHT)
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
