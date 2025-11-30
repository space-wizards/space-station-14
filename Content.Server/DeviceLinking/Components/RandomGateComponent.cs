using Content.Shared.DeviceLinking;
using Content.Server.DeviceLinking.Systems;
using Robust.Shared.Prototypes;

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
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> InputPort = "RandomGateInput";

    /// <summary>
    /// The output port for sending signals.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    /// <summary>
    /// The last output state of the gate.
    /// </summary>
    [DataField]
    public bool LastOutput;
}
