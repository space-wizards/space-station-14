using Content.Shared.DeviceLinking.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// Memory cell that sets the output to the input when enabled.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), Access(typeof(MemoryCellSystem))]
public sealed partial class MemoryCellComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "MemoryInput";

    /// <summary>
    /// Name of the enable port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> EnablePort = "MemoryEnable";

    /// <summary>
    /// Name of the output port.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "Output";

    // State
    /// <summary>
    /// Most recent state received on the input port.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SignalState InputState = SignalState.Low;

    /// <summary>
    /// Most recent state received on the enable port.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SignalState EnableState = SignalState.Low;

    /// <summary>
    /// Whether the cell's last output was high.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LastOutput;
}
