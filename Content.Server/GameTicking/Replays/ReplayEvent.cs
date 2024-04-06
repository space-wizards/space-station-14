using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Replays;

/// <summary>
/// Replay events are events that happen during the round formatted in a way that helps external tools to read them without needing to know the specifics of the fork.
/// </summary>
[Serializable, DataDefinition]
public partial class ReplayEvent
{
    [DataField]
    public uint Time;

    [DataField]
    public ReplayEventSeverity Severity;

    [DataField]
    public ReplayEventType EventType;
}

/// <summary>
/// A generic player event that can be used for any player-related event.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class GenericPlayerEvent : ReplayEvent
{
    /// <summary>
    /// The player info associated with this event. This who this event is about.
    /// </summary>
    /// <remarks>
    /// Storing the player name etc. every time is a bit wasteful, but since replays are already big as fuck, it's not that big of a deal.
    /// </remarks>
    [DataField]
    public ReplayEventPlayer Target;

    /// <summary>
    /// The source of the event. Who was the cause for the Target being affected? Can be null.
    /// </summary>
    [DataField]
    public ReplayEventPlayer? Origin;
}

/// <summary>
/// A generic event used for object or non player entites. It does not contain player info, and only strings.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class GenericObjectEvent : ReplayEvent
{
    [DataField]
    public string Target;

    [DataField]
    public string? Origin;
}
