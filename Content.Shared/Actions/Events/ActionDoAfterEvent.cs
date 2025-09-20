using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions.Events;

/// <summary>
/// The event that triggers when an action doafter is completed or cancelled
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ActionDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The action performer
    /// </summary>
    public readonly NetEntity Performer;

    /// <summary>
    /// The original action use delay, used for repeating actions
    /// </summary>
    public readonly TimeSpan? OriginalUseDelay;

    /// <summary>
    /// The original request, for validating
    /// </summary>
    public readonly RequestPerformActionEvent Input;

    public ActionDoAfterEvent(NetEntity performer, TimeSpan? originalUseDelay, RequestPerformActionEvent input)
    {
        Performer = performer;
        OriginalUseDelay = originalUseDelay;
        Input = input;
    }

    public override DoAfterEvent Clone() => this;
}
