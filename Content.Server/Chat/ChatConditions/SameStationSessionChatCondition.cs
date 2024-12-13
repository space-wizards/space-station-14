using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class SameStationSessionChatCondition : SessionChatCondition
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (!channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity))
            return new HashSet<ICommonSession>();

        var stationSystem = _entitySystem.GetEntitySystem<StationSystem>();
        var station = stationSystem.GetOwningStation((EntityUid)senderEntity);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return new HashSet<ICommonSession>();
        }

        if (!_entityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp))
            return new HashSet<ICommonSession>();

        var filter = stationSystem.GetInStation(stationDataComp);

        var resultingConsumers = filter.Recipients.Intersect(consumers);

        return resultingConsumers.ToHashSet();
    }
}
