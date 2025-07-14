using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.ParrotMessages;

[Serializable, NetSerializable]
public sealed class ParrotMessagesEuiState(List<ExtendedPlayerMessage> messages) : EuiStateBase
{
    public List<ExtendedPlayerMessage> Messages { get; } = messages;
}

[Serializable, NetSerializable]
public sealed class ParrotMessageRefreshMsg(bool showBlocked, bool currentRoundOnly) : EuiMessageBase
{
    public bool ShowBlocked { get; } = showBlocked;
    public bool CurrentRoundOnly { get; } = currentRoundOnly;
}

[Serializable, NetSerializable]
public sealed class ParrotMessageBlockChangeMsg(int messageId, bool block) : EuiMessageBase
{
    public int MessageId { get; } = messageId;
    public bool Block { get; } = block;
}

[Serializable, NetSerializable]
public sealed class ParrotMessageFilterChangeMsg(string filterString) : EuiMessageBase
{
    public string FilterString { get; } = filterString;
}
