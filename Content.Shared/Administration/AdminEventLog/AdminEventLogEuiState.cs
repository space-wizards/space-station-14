using Content.Shared.Eui;

namespace Content.Shared.Administration.AdminEventLog;

public sealed class AdminEventLogEuiState : EuiStateBase
{
    public AdminEventLogEuiState(int roundId)
    {
        RoundId = roundId;
    }

    public int RoundId { get; }
}
