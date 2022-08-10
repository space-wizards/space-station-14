using Robust.Shared.Serialization;

namespace Content.Shared.PneumaticCannon
{
    [Serializable, NetSerializable]
    public enum PneumaticCannonVisualLayers : byte
    {
        Base,
        Tank
    }

    [Serializable, NetSerializable]
    public enum PneumaticCannonVisuals
    {
        Tank
    }
}
