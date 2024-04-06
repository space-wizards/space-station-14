namespace Content.Server.GameTicking.Replays;

/// <summary>
/// Used to identify the severity of a replay event. <see cref="ReplayEvent"/>
/// </summary>
[Serializable]
public enum ReplayEventSeverity
{
    /// <summary>
    /// This is something that happens a lot and is not very important. For example someone cleans a puddle.
    /// </summary>
    Low,
    /// <summary>
    /// This is a bit higher than low, but still not very important. For example, a announcement is made.
    /// </summary>
    Medium,
    /// <summary>
    /// This is high shit, for example alert level changes to red or something.
    /// </summary>
    High,
    /// <summary>
    /// Station ending events, like the nuke being detonated. Or deaths.
    /// </summary>
    VeryHigh,
}
