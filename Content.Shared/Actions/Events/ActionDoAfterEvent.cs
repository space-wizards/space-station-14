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
    [NonSerialized]
    public readonly Entity<ActionsComponent?> Performer;

    public readonly TimeSpan? OriginalUseDelay;

    public ActionDoAfterEvent(Entity<ActionsComponent?> performer, TimeSpan? originalUseDelay)
    {
        Performer = performer;
        OriginalUseDelay = originalUseDelay;
    }

    public override DoAfterEvent Clone() => this;
}
