using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// Sends out a signal to machine linked objects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SignallerComponent : Component
{
    /// <summary>
    ///     The port that gets signaled when the switch turns on.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "Pressed";
}
