using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class IsGhostSessionChatCondition : SessionChatCondition
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        foreach (var consumer in consumers)
        {
            Logger.Debug("ISGHOST: " + _entityManager.HasComponent<GhostComponent>(consumer.AttachedEntity).ToString());
        }

        return consumers.Where(x => _entityManager.HasComponent<GhostComponent>(x.AttachedEntity)).ToHashSet();
    }
}
