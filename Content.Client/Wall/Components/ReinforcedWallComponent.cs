using Content.Client.IconSmoothing;

namespace Content.Client.Wall.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IconSmoothComponent))]
    public sealed class ReinforcedWallComponent : IconSmoothComponent // whyyyyyyyyy
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("reinforcedBase")]
        public string? ReinforcedStateBase;
    }

    public enum ReinforcedCornerLayers : byte
    {
        SE,
        NE,
        NW,
        SW,
    }
}
