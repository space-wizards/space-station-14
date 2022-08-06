using Robust.Shared.Serialization;

namespace Content.Shared.AME
{
    [Virtual]
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
