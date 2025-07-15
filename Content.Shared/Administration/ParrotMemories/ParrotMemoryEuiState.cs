using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.ParrotMemories;

[Serializable, NetSerializable]
public sealed class ParrotMemoryEuiState(List<ExtendedPlayerMessage> messages, int roundId) : EuiStateBase
{
    public List<ExtendedPlayerMessage> Messages { get; } = messages;
    public int RoundId = roundId;
}

[Serializable, NetSerializable]
public sealed class ParrotMemoryRefreshMsg(bool showBlocked, bool currentRoundOnly, string filterString) : EuiMessageBase
{
    public bool ShowBlocked { get; } = showBlocked;
    public bool CurrentRoundOnly { get; } = currentRoundOnly;
    public string FilterString { get; } = filterString;
}

[Serializable, NetSerializable]
public sealed class SetParrotMemoryBlockedMsg(int messageId, bool block) : EuiMessageBase
{
    public int MessageId { get; } = messageId;
    public bool Block { get; } = block;
}
