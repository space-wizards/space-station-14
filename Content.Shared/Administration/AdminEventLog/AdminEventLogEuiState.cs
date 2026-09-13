using Content.Shared.Eui;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.AdminEventLog;

[Serializable, NetSerializable]
public sealed class AdminEventLogEuiState : EuiStateBase
{
    public AdminEventLogEuiState(int roundId, bool allowNoneCategory)
    {
        RoundId = roundId;
        AllowNoneCategory = allowNoneCategory;
    }

    public int RoundId { get; }
    public bool AllowNoneCategory { get; }
}

[Serializable, NetSerializable]
public sealed class AdminEventLogEuiMsg : EuiMessageBase
{
    public AdminEventLogEuiMsg(int roundId, string adminUser, string eventDescription, ProtoId<EventCategoryPrototype>? category)
    {
        RoundId = roundId;
        AdminUser = adminUser;
        EventDescription = eventDescription;
        EventCategory = category;
    }

    public int RoundId { get; }
    public string AdminUser { get; }
    public string EventDescription { get; }

    public ProtoId<EventCategoryPrototype>? EventCategory { get; }
}
