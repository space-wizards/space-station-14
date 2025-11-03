namespace Content.Shared.Chat;

/// <summary>
/// Event fired before a player's entity speaks on LOOC
/// </summary>
[ByRefEvent]
public record struct LoocSpeakAttemptEvent(bool Cancelled = false);

/// <summary>
/// Event fired before a player's entity speaks on Ghostchat
/// </summary>
[ByRefEvent]
public record struct DeadChatSpeakAttemptEvent(bool Cancelled = false);
