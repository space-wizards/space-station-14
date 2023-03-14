using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

/// <summary>
///     Base type for events that get raised when a do-after is canceled or finished.
/// </summary>
[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract class DoAfterEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The do after that triggered this event. This will be set by the do after system before the event is raised.
    /// </summary>
    [NonSerialized]
    public DoAfter DoAfter = default!;

    /// <summary>
    ///     Duplicate the current event. This is used by state handling, and should copy by value unless the reference
    ///     types are immutable.
    /// </summary>
    public abstract DoAfterEvent Clone();

    #region Convenience properties
    public bool Cancelled => DoAfter.Cancelled;
    public EntityUid User => DoAfter.Args.User;
    public EntityUid? Target => DoAfter.Args.Target;
    public EntityUid? Used => DoAfter.Args.Used;
    public DoAfterArgs Args => DoAfter.Args;
    #endregion
}

/// <summary>
///     Blank / empty event for simple do afters that carry no information.
/// </summary>
/// <remarks>
///     This just exists as a convenience to avoid having to re-implement Clone() for every simply DoAfterEvent.
///     If an event actually contains data, it should actually override Clone().
/// </remarks>
[Serializable, NetSerializable]
public abstract class SimpleDoAfterEvent : DoAfterEvent
{
    // TODO: Find some way to enforce that inheritors don't store data?
    // Alternatively, I just need to allow generics to be networked.
    // E.g., then a SimpleDoAfter<TEvent> would just raise a TEvent event.
    // But afaik generic event types currently can't be serialized for networking or YAML.

    public override DoAfterEvent Clone() => this;
}

// Placeholder for obsolete async do afters
[Serializable, NetSerializable]
[Obsolete("Dont use async DoAfters")]
public sealed class AwaitedDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
///     This event will optionally get raised every tick while a do-after is in progress to check whether the do-after
///     should be canceled.
/// </summary>
public sealed class DoAfterAttemptEvent<TEvent> : CancellableEntityEventArgs where TEvent : DoAfterEvent
{
    /// <summary>
    ///     The do after that triggered this event.
    /// </summary>
    public readonly DoAfter DoAfter;

    /// <summary>
    ///     The event that the DoAfter will raise after sucesfully finishing. Given that this event has the data
    ///     required to perform the interaction, it should also contain the data required to validate/attempt the
    ///     interaction.
    /// </summary>
    public readonly TEvent Event;

    public DoAfterAttemptEvent(DoAfter doAfter, TEvent @event)
    {
        DoAfter = doAfter;
        Event = @event;
    }
}
