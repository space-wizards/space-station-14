using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class RangeChatCondition : ChatCondition
{
    public override Type? ConsumerType { get; set; } = typeof(EntityUid);

    /// <summary>
    /// The minimum range to meet this condition; inclusive.
    /// </summary>
    [DataField]
    public float MinimumRange = 0f;

    /// <summary>
    /// The maximum range to meet this condition; exclusive.
    /// </summary>
    [DataField]
    public float MaximumRange = 5f;

    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        if (consumers is HashSet<EntityUid> entityConsumers)
        {
            IoCManager.InjectDependencies(this);

            if (channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity) &&
                _entityManager.TryGetComponent<TransformComponent>((EntityUid)senderEntity, out var sourceTransform))
            {
                var returnConsumers = new HashSet<EntityUid>();

                foreach (var entity in entityConsumers)
                {
                    if (_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                    {
                        if (transform.MapID != sourceTransform.MapID)
                            continue;

                        // If you wanted to do something like a hard-of-hearing trait, our hearing extension component,
                        // this is probably where you'd check for it.

                        // Even if they are a ghost hearer, in some situations we still need the range
                        if (sourceTransform.Coordinates.TryDistance(_entityManager,
                                transform.Coordinates,
                                out var distance) &&
                            distance < MaximumRange &&
                            distance >= MinimumRange)
                        {
                            returnConsumers.Add(entity);
                        }
                    }
                }

                return returnConsumers as HashSet<T> ?? new HashSet<T>();
            }
        }

        return new HashSet<T>();
    }
}
