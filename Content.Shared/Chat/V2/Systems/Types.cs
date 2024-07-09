using Robust.Shared.Network;

namespace Content.Shared.Chat.V2.Systems;

/// <summary>
/// The record associated with a specific chat event.
/// </summary>
public struct ChatRecord(string userName, NetUserId userId, ICreatedChatEvent storedEvent, string entityName)
{
    public string UserName = userName;
    public NetUserId UserId = userId;
    public string EntityName = entityName;
    public ICreatedChatEvent StoredEvent = storedEvent;
}
