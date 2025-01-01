using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class SameStationChatCondition : ChatCondition
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        if (consumers is HashSet<ICommonSession> sessionConsumers)
        {
            IoCManager.InjectDependencies(this);

            if (!channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity))
                return new HashSet<T>();

            var stationSystem = _entitySystem.GetEntitySystem<StationSystem>();
            var station = stationSystem.GetOwningStation((EntityUid)senderEntity);

            if (station == null)
            {
                // you can't make a station announcement without a station
                return new HashSet<T>();
            }

            if (!_entityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp))
                return new HashSet<T>();

            var filter = stationSystem.GetInStation(stationDataComp);

            var resultingConsumers = filter.Recipients.Intersect(sessionConsumers);

            return resultingConsumers.ToHashSet() as HashSet<T> ?? new HashSet<T>();
        }

        return new HashSet<T>();
    }
}
