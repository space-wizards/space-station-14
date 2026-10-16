using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

public interface IPdaMessengerUiMessagePayload;

/// <summary>
///     Sends a message to a user.
/// </summary>
[Serializable, NetSerializable]
public sealed record PdaMessengerSendMessage(string RecipientId, string Text) : IPdaMessengerUiMessagePayload;

/// <summary>
///     Opens a conversation, which marks it read, or closes it when null.
/// </summary>
[Serializable, NetSerializable]
public sealed record PdaMessengerOpenConversation(string? ConversationId) : IPdaMessengerUiMessagePayload;

[Serializable, NetSerializable]
public sealed class PdaMessengerUiMessageEvent(IPdaMessengerUiMessagePayload payload) : CartridgeMessageEvent
{
    public readonly IPdaMessengerUiMessagePayload Payload = payload;
}
