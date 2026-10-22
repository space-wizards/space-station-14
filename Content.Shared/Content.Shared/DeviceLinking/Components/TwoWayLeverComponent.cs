using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// Simple ternary state for device linking.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class TwoWayLeverComponent : Component
{
    /// <summary>
    /// The current position of the lever.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TwoWayLeverState State;

    /// <summary>
    /// Whether the next activation sends a signal through the left port.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NextSignalLeft;

    /// <summary>
    /// The output port used for the left lever position.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LeftPort = "Left";

    /// <summary>
    /// The output port used for the right lever position.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> RightPort = "Right";

    /// <summary>
    /// The output port used for the middle lever position.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> MiddlePort = "Middle";
}
