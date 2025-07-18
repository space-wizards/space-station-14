using Content.Shared.Administration.PlayerMessage;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.ParrotMemories;

/// <summary>
/// Sets the state of the UI
/// </summary>
[Serializable, NetSerializable]
public sealed class ParrotMemoryEuiState(List<ExtendedPlayerMessage> messages, int currentRoundId, int messagesRoundId) : EuiStateBase
{
    public List<ExtendedPlayerMessage> Messages { get; } = messages;
    public int CurrentRoundId = currentRoundId;
    public int MessagesRoundId = messagesRoundId;
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class ParrotMemoryRefreshMsg(bool showBlocked, string filterString, int? requestedRoundId) : EuiMessageBase
{
    public bool ShowBlocked { get; } = showBlocked;
    public string FilterString { get; } = filterString;
    public int? RequestedRoundId { get; } = requestedRoundId;
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class SetParrotMemoryBlockedMsg(int messageId, bool block) : EuiMessageBase
{
    public int MessageId { get; } = messageId;
    public bool Block { get; } = block;
}
