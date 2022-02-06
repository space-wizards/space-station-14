using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.AME
{
    public class SharedAMEShieldComponent : Component
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
