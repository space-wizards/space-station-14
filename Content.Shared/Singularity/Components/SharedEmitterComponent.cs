using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components
{
    [NetSerializable, Serializable]
    public enum EmitterVisuals
    {
        VisualState
    }

    [NetSerializable, Serializable]
    public enum EmitterVisualState
    {
        On,
        Underpowered,
        Off
    }
}
