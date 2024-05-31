using Robust.Shared.Serialization;

namespace Content.Shared.Light
{
    [Serializable, NetSerializable]
    public enum PoweredLightVisuals : byte
    {
        BulbState,
        Blinking
    }

    [Serializable, NetSerializable]
    public enum PoweredLightState : byte
    {
        Empty,
        On,
        Off,
        Broken,
        Burned
    }

    public enum PoweredLightLayers : byte
    {
        Base,
        Glow
    }
}
