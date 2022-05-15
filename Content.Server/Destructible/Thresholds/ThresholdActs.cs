using Robust.Shared.Serialization;

namespace Content.Server.Destructible.Thresholds
{
    [Flags, FlagsFor(typeof(ActsFlags))]
    [Serializable]
    public enum ThresholdActs
    {
        None = 0,
        Breakage,
        Destruction
    }
}
