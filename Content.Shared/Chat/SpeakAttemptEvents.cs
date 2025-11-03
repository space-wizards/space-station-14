namespace Content.Shared.Chat;

/// <summary>
/// Event fired before a player's entity speaks on LOOC or Deadchat.
/// </summary>
[ByRefEvent]
public record struct LoocSpeakAttemptEvent(InGameOOCChatType Type, bool Cancelled = false);
