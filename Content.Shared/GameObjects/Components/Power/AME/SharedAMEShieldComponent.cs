using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power.AME
{
    public abstract class SharedAMEShieldComponent : Component
    {
        [Serializable, NetSerializable]
        public enum AMEShieldVisuals
        {
            Core,
            CoreState
        }

        public enum AMECoreState
        {
            Off,
            Weak,
            Strong
        }
    }
}
