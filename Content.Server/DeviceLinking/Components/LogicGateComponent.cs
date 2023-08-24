using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// A logic gate that sets its output port by doing an operation on its 2 input ports, A and B.
/// </summary>
[RegisterComponent]
[Access(typeof(LogicGateSystem))]
public sealed partial class LogicGateComponent : Component
{
    /// <summary>
    /// The logic gate operation to use.
    /// </summary>
    [DataField("gate")]
    public LogicGate Gate = LogicGate.Or;

    /// <summary>
    /// Tool quality to use for cycling logic gate operations.
    /// Cannot be pulsing since linking uses that.
    /// </summary>
    [DataField("cycleQuality", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string CycleQuality = "Screwing";

    /// <summary>
    /// Sound played when cycling logic gate operations.
    /// </summary>
    [DataField("cycleSound")]
    public SoundSpecifier CycleSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    /// <summary>
    /// Name of the first input port.
    /// </summary>
    [DataField("inputPortA", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string InputPortA = "InputA";

    /// <summary>
    /// Name of the second input port.
    /// </summary>
    [DataField("inputPortB", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string InputPortB = "InputB";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField("outputPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string OutputPort = "Output";

    // Initial state
    [ViewVariables]
    public SignalState StateA = SignalState.Low;

    [ViewVariables]
    public SignalState StateB = SignalState.Low;

    [ViewVariables]
    public bool LastOutput;
}

/// <summary>
/// Last state of a signal port, used to not spam invoking ports.
/// </summary>
public enum SignalState : byte
{
    Momentary, // Instantaneous pulse high, compatibility behavior
    Low,
    High
}
