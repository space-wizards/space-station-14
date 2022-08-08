using Robust.Shared.Serialization;

namespace Content.Shared.Cloning
{
    [Serializable, NetSerializable]
    public enum CloningPodVisuals : byte
    {
        Status
    }
    [Serializable, NetSerializable]
    public enum CloningPodStatus : byte
    {
        Idle,
        Cloning,
        Gore,
        NoMind
    }
}
