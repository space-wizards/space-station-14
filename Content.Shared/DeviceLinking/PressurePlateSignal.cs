using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking
{
    [Serializable, NetSerializable]
    public enum PressurePlateVisuals : byte
    {
        State
    }

    [Serializable, NetSerializable]
    public enum PressurePlateState : byte
    {
        Pressed,
        Released
    }
}
