using Content.Shared.Chat.V2.Systems;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Defines a generic chat event.
/// </summary>
public interface IChatEvent
{
    public ChatContext Context
    {
        get;
    }

    /// <summary>
    /// The sender of the chat message.
    /// </summary>
    public EntityUid Sender
    {
        get;
    }

    /// <summary>
    /// The ID of the message. This is overwritten when saved into a repository.
    /// </summary>
    public uint Id
    {
        get;
        set;
    }

    /// <summary>
    /// The sent message.
    /// </summary>
    public string Message
    {
        get;
        set;
    }

    public void SetId(uint id)
    {
        if (Id != 0)
        {
            return;
        }

        Id = id;
    }
}

// TODO: These three channel enums can be migrated to YAML instead of being defined here.

/// <summary>
/// Covers the types of verbal chat message.
/// </summary>
public enum VerbalChatChannel : byte
{
    // Thought, not spoken, and thus totally silent. Used for telepathic-like communications using other systems like radio.
    Internal,
    // Short-range, obfuscated if too far away, no voice ID if too far away and no visuals
    Whisper,
    // Normal range.
    Talk,
    // Normal range but BIGGER TEXT!!
    Shout,
    // For breaking the fourth wall. Works just like Talk but the message is flagged as OOC.
    OutOfCharacter,
    // Used by vending machines and other automated talking machines. This channel shouldn't show up in chat logs.
    Background
}

/// <summary>
/// Covers the types of visual chat message
/// </summary>
public enum VisualChatChannel : byte
{
    // Used to smile, laugh, and evade the mime vow.
    Emote
}

public enum OutOfCharacterChatChannel : byte
{
    Dead,
    OutOfCharacter,
    Admin,
    System
}
