using Content.Shared.DeviceLinking.Systems;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// A logic gate that sets its output port by doing an operation on its 2 input ports, A and B.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(LogicGateSystem))]
public sealed partial class LogicGateComponent : Component
{
    /// <summary>
    /// The logic gate operation to use.
    /// </summary>
    [DataField]
    public LogicGate Gate = LogicGate.Or;

    /// <summary>
    /// Tool quality to use for cycling logic gate operations.
    /// Cannot be pulsing since linking uses that.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> CycleQuality = "Screwing";

    /// <summary>
    /// Sound played when cycling logic gate operations.
    /// </summary>
    [DataField]
    public SoundSpecifier CycleSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    /// <summary>
    /// Name of the first input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPortA = "InputA";

    /// <summary>
    /// Name of the second input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPortB = "InputB";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    // Initial state, used to not spam invoke ports
    [DataField, AutoNetworkedField]
    public SignalState StateA = SignalState.Low;

    [DataField, AutoNetworkedField]
    public SignalState StateB = SignalState.Low;

    [DataField, AutoNetworkedField]
    public bool LastOutput;
}
