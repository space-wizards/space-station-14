using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedHandheldLightComponent : Component
    {
        public sealed override string Name => "HandheldLight";
        public sealed override uint? NetID => ContentNetIDs.HANDHELD_LIGHT;

        [Serializable, NetSerializable]
        protected sealed class HandheldLightComponentState : ComponentState
        {
            public HandheldLightComponentState(float? charge) : base(ContentNetIDs.HANDHELD_LIGHT)
            {
                Charge = charge;
            }

            public float? Charge { get; }
        }
    }

    [Serializable, NetSerializable]
    public enum HandheldLightVisuals
    {
        FullPower,
        LowPower,
        Dying
    }


}
