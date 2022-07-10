using JetBrains.Annotations;

namespace Content.Server.StationEvents;

/// <summary>
///     This system handles adding, removing, starting, stopping, announcing, etc station events.
///     This system does not handle how these events are started or stopped. That is up to game rules
///     (like <see cref="BasicStationEventSchedulerSystem"/>)
/// </summary>
[PublicAPI]
public sealed class StationEventManagementSystem : EntitySystem
{
    private readonly HashSet<string> _activeEvents = new();
    private readonly Dictionary<TimeSpan, string> _allEvents = new();

    /// <summary>
    ///     A set containing the current active events prototype IDs.
    /// </summary>
    /// <remarks>
    ///     Some station events can be instantaneous (essentially) while others are long-running. It's up to event systems
    ///     to manage their own lifetime and to shutdown when necessary, as there is no default behavior for this.
    /// </remarks>
    public IReadOnlySet<string> ActiveEvents => _activeEvents;

    /// <summary>
    ///     A dictionary containing all events that have been run this round, keyed with their start time.
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    public IReadOnlyDictionary<TimeSpan, string> AllEvents => _allEvents;

    #region Active Events API

    public bool StartStationEvent(string prototype)
    {
        throw new NotImplementedException();
    }

    public bool StopStationEvent(string prototype)
    {
        throw new NotImplementedException();
    }

    public bool IsStationEventRunning(string prototype)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region All Events API

    public int TimesEventRun(string prototype)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Returns the time since the last specified even prototype was runt. If no event prototype is specified,
    ///     returns the time since any event occurred.
    /// </summary>
    public TimeSpan TimeSinceLastEvent(string? prototype)
    {
        throw new NotImplementedException();
    }

    #endregion
}

/// <summary>
///     Raised broadcast when a new station event has been started, so that systems may handle it
///     and perform any necessary startup logic.
/// </summary>
public sealed class StationEventStartedEvent : StationEventEvent
{
    public StationEventStartedEvent(string prototype) : base(prototype) {
    }
}

/// <summary>
///     Raised broadcast when a station event has been stopped, so that systems may handle it
///     and perform any necessary cleanup logic.
/// </summary>
public sealed class StationEventStoppedEvent : StationEventEvent
{
    public StationEventStoppedEvent(string prototype) : base(prototype) {
    }
}

public abstract class StationEventEvent : EntityEventArgs
{
    public string Prototype;

    protected StationEventEvent(string prototype)
    {
        Prototype = prototype;
    }
}
