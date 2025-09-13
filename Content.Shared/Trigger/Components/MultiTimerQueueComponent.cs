using Robust.Shared.GameStates;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiTimerQueueComponent : BaseXOnTriggerComponent
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Queue<string> Queue = new();
    [DataField(required: true)]
    [AutoNetworkedField]
    public Queue<TimeSpan> QueueDelays = new();
}
