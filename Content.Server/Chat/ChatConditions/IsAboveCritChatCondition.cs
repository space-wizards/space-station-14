using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Microsoft.Extensions.Logging;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer is alive and above crit; does not check for consciousness e.g. sleeping.
/// </summary>
[DataDefinition]
public sealed partial class IsAboveCritChatCondition : ChatCondition
{
    public override Type? ConsumerType { get; set; } = typeof(EntityUid);

    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        if (consumers is HashSet<EntityUid> entityConsumers)
        {
            IoCManager.InjectDependencies(this);

            if (_entitySystem.TryGetEntitySystem<MobStateSystem>(out var mobStateSystem))
            {
                var filteredEntities = entityConsumers.Where(x => !mobStateSystem.IsIncapacitated(x)).ToHashSet();
                return filteredEntities as HashSet<T> ?? new HashSet<T>();
            }
        }

        return new HashSet<T>();
    }
}
