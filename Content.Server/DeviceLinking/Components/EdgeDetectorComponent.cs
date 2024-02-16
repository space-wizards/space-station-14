using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// An edge detector that pulses high or low output ports when the input port gets a rising or falling edge respectively.
/// </summary>
[RegisterComponent, Access(typeof(EdgeDetectorSystem))]
public sealed partial class EdgeDetectorComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> InputPort = "Input";

    /// <summary>
    /// Name of the rising edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> OutputHighPort = "OutputHigh";

    /// <summary>
    /// Name of the falling edge output port.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> OutputLowPort = "OutputLow";

    // Initial state
    [DataField]
    public SignalState State = SignalState.Low;
}
