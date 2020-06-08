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
            public override uint NetID => ContentNetIDs.HANDHELD_LIGHT;

            public HandheldLightComponentState(float? charge)
            {
                Charge = charge;
            }

            public float? Charge { get; }
        }
    }
}
