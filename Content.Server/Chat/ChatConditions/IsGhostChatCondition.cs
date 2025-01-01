using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class IsGhostChatCondition : ChatCondition
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        if (consumers is HashSet<ICommonSession> sessionConsumers)
        {
            IoCManager.InjectDependencies(this);

            var filteredSessions = sessionConsumers.Where(x => _entityManager.HasComponent<GhostComponent>(x.AttachedEntity)).ToHashSet();
            return filteredSessions as HashSet<T> ?? new HashSet<T>();
        }

        return new HashSet<T>();
    }
}
