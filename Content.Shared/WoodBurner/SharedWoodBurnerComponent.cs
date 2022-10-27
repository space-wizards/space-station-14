using Robust.Shared.Serialization;

namespace Content.Shared.WoodBurner
{
    [Serializable, NetSerializable]
    public enum WoodBurnerVisuals : byte
    {
        Powered,
        Processing,
    }
}
