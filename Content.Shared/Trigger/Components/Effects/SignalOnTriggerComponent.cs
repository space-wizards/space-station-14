using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Sends a device link signal when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SignalOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The port that gets signaled when the switch turns on.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "Trigger";
}
