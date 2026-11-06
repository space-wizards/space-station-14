using Content.Shared.PdaMessenger;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public enum PdaMessengerStatus : byte
{
    Connected,
    NoId,
    NoServer,
    Disabled,
}

/// <summary>
///     What the messenger program shows: online users and the inserted ID card's conversations.
/// </summary>
/// <param name="unread">Unread message counts, by conversation ID.</param>
/// <param name="sendBlocked">Why the user can't send, e.g. an admin mute, or null if they can.</param>
[Serializable, NetSerializable]
public sealed class PdaMessengerUiState(
    PdaMessengerStatus status,
    PdaMessengerContact? self,
    List<PdaMessengerContact> contacts,
    List<PdaMessengerConversation> conversations,
    Dictionary<string, int> unread,
    LocId? sendBlocked) : BoundUserInterfaceState
{
    public readonly PdaMessengerStatus Status = status;
    public readonly PdaMessengerContact? Self = self;
    public readonly List<PdaMessengerContact> Contacts = contacts;
    public readonly List<PdaMessengerConversation> Conversations = conversations;
    public readonly Dictionary<string, int> Unread = unread;
    public readonly LocId? SendBlocked = sendBlocked;
}
