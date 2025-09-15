using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Sends a trigger when signal is received.
/// The user is the sender of the signal.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnSignalComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The sink port prototype we can connect devices to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SinkPortPrototype> Port = "Trigger";
}
