using Robust.Shared.Serialization;

namespace Content.Shared.Radio
{
    [Serializable, NetSerializable]
    public enum TelecommsMachineVisuals : byte
    {
        IsOn,
        IsTransmiting
    }
}
