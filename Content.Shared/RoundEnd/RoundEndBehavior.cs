namespace Content.Shared.RoundEnd;

public enum RoundEndBehavior : byte
{
    /// <summary>
    /// Instantly end round
    /// </summary>
    InstantEnd,

    /// <summary>
    /// Call shuttle with custom announcement
    /// </summary>
    ShuttleCall,

    /// <summary>
    /// Do nothing
    /// </summary>
    Nothing
}
