using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using System.ComponentModel.DataAnnotations;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// A component for a random gate, which outputs a signal with a 50% probability.
/// </summary>
[RegisterComponent, Access(typeof(RandomGateSystem))]
public sealed partial class RandomGateComponent : Component
{
    /// <summary>
    /// The input port for receiving signals.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "RandomGateInput";

    /// <summary>
    /// The output port for sending signals.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    /// <summary>
    /// The last output state of the gate.
    /// </summary>
    [DataField]
    public bool LastOutput;

    /// <summary>
    /// The probability (0.0 to 1.0) that the gate will output a signal.
    /// </summary>
    [DataField]
    [Range(0f, 1f)]
    public float SuccessProbability = 0.5f;
}
