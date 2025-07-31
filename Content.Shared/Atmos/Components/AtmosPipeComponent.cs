using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AtmosPipeComponent : Component;

[ByRefEvent]
public record struct AtmosPipeColorChangedEvent(Color color)
{
    public Color Color = color;
}

[Serializable, NetSerializable]
public enum PipeVisualLayers : byte
{
    Pipe,
}
