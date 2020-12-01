using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power.AME
{
    public class SharedAMEShieldComponent : Component
    {
        public override string Name => "AMEShield";

        [Serializable, NetSerializable]
        public enum AMEShieldVisuals : byte
        {
            Core,
            CoreState
        }

        public enum AMECoreState : byte
        {
            Off,
            Weak,
            Strong
        }
    }
}
