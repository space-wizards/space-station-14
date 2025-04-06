using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.Instruments;

/// <summary>
/// Controls the bool <see cref="InstrumentVisuals"/> when the instrument UI is open.
/// Use GenericVisualizerComponent to then control sprite states.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InstrumentVisualsComponent : Component;

[Serializable, NetSerializable]
public enum InstrumentVisuals : byte
{
    Playing,
    Layer
}
