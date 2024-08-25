using System.Numerics;
using Content.Shared.Mobs;

namespace Content.Server.GameTicking.Replays;

/// <summary>
/// Represents an event that occured during gameplay, such as a chat message, a player joining, etc.
/// with the goal of being able to reconstruct a round without actually running the replay (so external tools parsing it, without knowing the specifics of the fork)
/// </summary>
[Serializable, DataDefinition]
public partial class ReplayEvent
{
    [DataField]
    public double? Time;

    [DataField]
    public ReplayEventSeverity Severity;

    [DataField]
    public ReplayEventType EventType;

    [DataField]
    public string? NearestBeacon;

    [DataField]
    public Vector2 Position;
};

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

[Serializable, DataDefinition]
public sealed partial class MobStateChangedPlayerReplayEvent : ReplayEvent
{
    [DataField]
    public ReplayEventPlayer Target;

    [DataField]
    public MobState OldState;

    [DataField]
    public MobState NewState;
}

[Serializable, DataDefinition]
public sealed partial class MobStateChangedNPCReplayEvent : ReplayEvent
{
    [DataField]
    public string Target;

    [DataField]
    public MobState OldState;

    [DataField]
    public MobState NewState;
}

[Serializable, DataDefinition]
public sealed partial class StoreBuyReplayEvent : ReplayEvent
{
    [DataField]
    public ReplayEventPlayer Buyer;

    [DataField]
    public string Item;

    [DataField]
    public int Cost;
}

[Serializable, DataDefinition]
public sealed partial class ReplayExplosionEvent : ReplayEvent
{
    [DataField]
    public ReplayEventPlayer? Source;

    [DataField]
    public float Intensity;

    [DataField]
    public float Slope;

    [DataField]
    public float MaxTileIntensity;

    [DataField]
    public float TileBreakScale;

    [DataField]
    public int MaxTileBreak;

    [DataField]
    public bool CanCreateVacuum;

    [DataField]
    public string Type;
}

[Serializable, DataDefinition]
public sealed partial class ChatAnnouncementReplayEvent : ReplayEvent
{
    [DataField]
    public string Message;

    [DataField]
    public string Sender;
}

[Serializable, DataDefinition]
public sealed partial class ChatMessageReplayEvent : ReplayEvent
{
    [DataField]
    public string Message;

    [DataField]
    public ReplayEventPlayer Sender;

    [DataField]
    public string Type;
}

[Serializable, DataDefinition]
public sealed partial class AlertLevelChangedReplayEvent : ReplayEvent
{
    [DataField]
    public string AlertLevel;
}

[Serializable, DataDefinition]
public sealed partial class NewsArticlePublishedReplayEvent : ReplayEvent
{
    [DataField]
    public string Title;

    [DataField]
    public string Content;

    [DataField]
    public string? Author;

    [DataField]
    public TimeSpan ShareTime;
}

[Serializable, DataDefinition]
public sealed partial class TechnologyUnlockedReplayEvent : ReplayEvent
{
    [DataField]
    public string Name;

    [DataField]
    public string Discipline;

    [DataField]
    public int Tier;

    [DataField]
    public ReplayEventPlayer Player;
}

[Serializable, DataDefinition]
public sealed partial class ShuttleReplayEvent : ReplayEvent
{
    [DataField]
    public int? Countdown;

    [DataField]
    public ReplayEventPlayer? Source;
}
