using Robust.Shared.Player;

namespace Content.Shared.Chat;

/// <summary>
/// Event fired before a player's entity speaks on LOOC or Deadchat.
/// </summary>
[ByRefEvent]
public record struct InGameOocMessageAttemptEvent(ICommonSession Session, InGameOOCChatType Type, bool Cancelled = false);
