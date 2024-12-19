using Robust.Shared.Serialization;

namespace Content.Shared.Destructible.Thresholds
{
    [Flags, FlagsFor(typeof(ActsFlags))]
    [Serializable, NetSerializable]
    public enum ThresholdActs
    {
        None = 0,
        Breakage,
        Destruction
    }

    public sealed class ActsFlags {}
}
