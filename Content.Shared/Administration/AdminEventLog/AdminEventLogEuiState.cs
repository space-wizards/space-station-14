using Content.Shared.Eui;
using Robust.Shared.Player;
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

[Serializable, NetSerializable]
public sealed class AdminEventLogEuiMsg : EuiMessageBase
{
    public AdminEventLogEuiMsg(int roundId, ICommonSession admin, string eventDescription)
    {
        RoundId = roundId;
        Admin = admin;
        EventDescription = eventDescription;
    }

    public int RoundId { get; }
    public ICommonSession Admin { get; }
    public string EventDescription { get; }
}
