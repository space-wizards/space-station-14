using Content.Shared.Actions.Components;
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
    /// <inheritdoc cref="ActionAttemptDoAfterEvent.Performer"/>
    /// </summary>
    public readonly NetEntity Performer;

    /// <summary>
    /// <inheritdoc cref="ActionAttemptDoAfterEvent.OriginalUseDelay"/>
    /// </summary>
    public readonly TimeSpan? OriginalUseDelay;

    /// <summary>
    /// <inheritdoc cref="ActionAttemptDoAfterEvent.Input"/>
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
