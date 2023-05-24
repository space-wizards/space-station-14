using Robust.Shared.Serialization;

namespace Content.Shared.AME;

[Virtual]
public class SharedAMEShieldComponent : Component
{
    [Serializable, NetSerializable]
    public enum AmeShieldVisuals
    {
        Core,
        CoreState
    }

    [Serializable, NetSerializable]
    public enum AmeCoreState
    {
        Off,
        Weak,
        Strong
    }
}
