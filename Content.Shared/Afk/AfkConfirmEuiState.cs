using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Afk;

[Serializable, NetSerializable]
public sealed class AfkConfirmEuiState(TimeSpan timeRemaining) : EuiStateBase
{
    public TimeSpan TimeRemaining { get; } = timeRemaining;
}

[Serializable, NetSerializable]
public sealed class AfkConfirmMessage : EuiMessageBase;
