using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class SameStationChatCondition : ChatCondition
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        return false;
    }

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext channelParameters)
    {
        if (!channelParameters.TryGet<EntityUid>(DefaultChannelParameters.SenderEntity, out var senderEntity))
            return false;

        IoCManager.InjectDependencies(this);
        
        var stationSystem = _entitySystem.GetEntitySystem<StationSystem>();
        var station = stationSystem.GetOwningStation(senderEntity);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return false;
        }

        if (!_entityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp))
            return false;

        var filter = stationSystem.GetInStation(stationDataComp);

        return filter.Recipients.Contains(subjectSession);
    }
}
