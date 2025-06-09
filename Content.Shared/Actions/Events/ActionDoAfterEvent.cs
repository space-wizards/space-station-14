using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions.Events;

/// <summary>
/// The event that triggers when an action doafter is completed or cancelled
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ActionDoAfterEvent : DoAfterEvent
{
    public readonly RequestPerformActionEvent RequestEvent;

    public ActionDoAfterEvent(RequestPerformActionEvent requestEvent)
    {
        RequestEvent = requestEvent;
    }

    public override DoAfterEvent Clone() => this;
}
