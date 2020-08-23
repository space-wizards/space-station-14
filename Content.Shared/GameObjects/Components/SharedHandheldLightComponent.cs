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

        [Serializable, NetSerializable]
        protected sealed class HandheldLightComponentState : ComponentState
        {
            public HandheldLightComponentState(float? charge, bool hasCell) : base(ContentNetIDs.HANDHELD_LIGHT)
            {
                Charge = charge;
                HasCell = hasCell;
            }

            public float? Charge { get; }

            public bool HasCell { get; }
        }
    }
}
