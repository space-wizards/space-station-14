using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    [Serializable, NetSerializable]
    public enum SolutionContainerVisuals : byte
    {
        Color,
        FillFraction,
    }

    public enum SolutionContainerLayers : byte
    {
        Fill,
        Base
    }
}
