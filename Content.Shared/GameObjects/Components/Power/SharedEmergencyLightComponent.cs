using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    public class SharedEmergencyLightComponent : Component
    {
        public override string Name => "EmergencyLight";

        public sealed override uint? NetID => ContentNetIDs.EMERGENCY_LIGHT;

        public enum EmergencyLightState
        {
            Charging,
            Full,
            Empty,
            On
        }

        [Serializable, NetSerializable]
        protected sealed class EmergencyLightComponentState : ComponentState
        {
            public EmergencyLightComponentState(EmergencyLightState state) : base(ContentNetIDs.EMERGENCY_LIGHT)
            {
                State = state;
            }

            public EmergencyLightState State { get; }
        }

    }
}
