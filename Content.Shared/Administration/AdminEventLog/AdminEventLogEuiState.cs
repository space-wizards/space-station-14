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
    public AdminEventLogEuiMsg(int roundId, string adminUser, string eventDescription)
    {
        RoundId = roundId;
        AdminUser = adminUser;
        EventDescription = eventDescription;
    }

    public int RoundId { get; }
    public string AdminUser { get; }
    public string EventDescription { get; }
}
