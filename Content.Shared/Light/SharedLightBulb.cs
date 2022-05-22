using Robust.Shared.Serialization;

namespace Content.Shared.Light
{
    [Serializable, NetSerializable]
    public enum LightBulbState : byte
    {
        Normal,
        Broken,
        Burned,
    }

    [Serializable, NetSerializable]
    public enum LightBulbVisuals : byte
    {
        State,
        Color
    }

    [Serializable, NetSerializable]
    public enum LightBulbType : byte
    {
        Bulb,
        Tube,
    }
}
