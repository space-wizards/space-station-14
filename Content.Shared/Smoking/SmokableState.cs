using Robust.Shared.Serialization;

namespace Content.Shared.Smoking
{
    [Serializable, NetSerializable]
    public enum SmokableState : byte
    {
        Unlit,
        Lit,
        Burnt,
    }
}
