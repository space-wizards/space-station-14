using Content.Shared.Chat;

namespace Content.Server.Chat;

public sealed class ChatUser
{
    /// <summary>
    ///     The unique key associated with this chat user, starting from 1 and incremented.
    ///     Used when the server sends <see cref="MsgChatMessage"/>.
    ///     Used on the client to delete messages sent by this user when receiving
    ///     <see cref="MsgDeleteChatMessagesBy"/>.
    /// </summary>
    public readonly int Key;

    /// <summary>
    ///     All entities that this chat user was attached to while sending chat messages.
    ///     Sent to the client to delete messages sent by those entities when receiving
    ///     <see cref="MsgDeleteChatMessagesBy"/>.
    /// </summary>
    public readonly HashSet<NetEntity> Entities = new();

    public ChatUser(int key)
    {
        Key = key;
    }

    public void AddEntity(NetEntity entity)
    {
        if (!entity.Valid)
            return;

        Entities.Add(entity);
    }
}
