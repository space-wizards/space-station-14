using Content.Shared.DeviceLinking.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// An edge detector that pulses high or low output ports when the input port gets a rising or falling edge respectively.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(EdgeDetectorSystem))]
public sealed partial class EdgeDetectorComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Input";

    /// <summary>
    /// Name of the rising edge output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputHighPort = "OutputHigh";

    /// <summary>
    /// Name of the falling edge output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputLowPort = "OutputLow";

    // Initial state
    [DataField, AutoNetworkedField]
    public SignalState State = SignalState.Low;
}
