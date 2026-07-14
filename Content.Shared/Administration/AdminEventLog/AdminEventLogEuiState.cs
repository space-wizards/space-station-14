using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.AdminEventLog;

[Serializable, NetSerializable]
public sealed class AdminEventLogEuiState : EuiStateBase
{
    public AdminEventLogEuiState(int roundId)
    {
        RoundId = roundId;
    }

    public int RoundId { get; }
}
