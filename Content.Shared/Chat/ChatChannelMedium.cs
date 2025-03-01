namespace Content.Shared.Chat;

/// <summary>
///     Represents which medium a chat channel uses to convey messages.
/// </summary>
[Flags]
public enum ChatChannelMedium : ushort
{
    None = 0,

    /// <summary>
    ///     Chat sent via sound in-character
    /// </summary>
    Auditory = 1 << 0,

    /// <summary>
    ///     Chat sent via motions and sight in-character
    /// </summary>
    Visual = 1 << 1,

    /// <summary>
    ///     Chat sent via mental methods in-character, e.g. psionics
    /// </summary>
    Mental = 1 << 2,

    /// <summary>
    ///     Chat sent via emotions and feelings, such as artifact effects and anomaly infections
    /// </summary>
    Emotional = 1 << 3,

    /// <summary>
    ///     Chat sent out of character, i.e. by a player
    /// </summary>
    OOC = 1 << 4,

    /// <summary>
    ///     Non-diagetic messages sent to the player by the game/server
    /// </summary>
    GameMessage = 1 << 5,

    /// <summary>
    ///     Channels considered to be IC.
    /// </summary>
    IC = Auditory | Visual | Mental,
}

