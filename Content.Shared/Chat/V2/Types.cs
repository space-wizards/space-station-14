using Content.Shared.Chat.V2.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Defines a chat event being passed around inside the chat process.
/// </summary>
public interface ICreatedChatEvent
{
    public ChatContext Context
    {
        get;
    }

    /// <summary>
    /// The sender of the chat message.
    /// </summary>
    public NetEntity Sender
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

    public ICommonSession SenderSession
    {
        get;
    }

    public ChatReceivedEvent ToReceivedEvent();

    public ICreatedChatEvent Clone();
}
