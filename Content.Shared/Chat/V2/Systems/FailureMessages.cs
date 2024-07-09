using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Systems;

/// <summary>
/// Defines the abstract concept of chat attempts failing.
/// </summary>
[Serializable, NetSerializable]
public abstract class ChatFailedEvent(ChatContext context, NetEntity sender, string? reason) : EntityEventArgs
{
    public ChatContext Context = context;
    public NetEntity Sender = sender;
    public string? Reason = reason;
}

/// <summary>
/// Raised when a character has failed to speak.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class VerbalChatFailedEvent(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);

/// <summary>
/// Raised when a mob has failed to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class VisualChatFailedEvent(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);

/// <summary>
/// Raised when an announcement is attempted by a communications console, and it fails for some reason.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class AnnouncementFailedEvent(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);

public sealed class OutOfCharacterChatFailed(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);
