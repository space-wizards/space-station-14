using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer is on a map with an active server, if necessary.
/// </summary>
[DataDefinition]
public sealed partial class HasComponentChatCondition : ChatCondition
{
    public override Type? ConsumerType { get; set; } = typeof(EntityUid);

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    [DataField]
    public string? Component;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        if (consumers is HashSet<EntityUid> entityConsumers)
        {
            IoCManager.InjectDependencies(this);

            var returnConsumers = new HashSet<EntityUid>();

            if (Component != null)
            {
                foreach (var consumer in entityConsumers)
                {
                    var comp = _componentFactory.GetRegistration(Component, true);
                    if (_entityManager.HasComponent(consumer, comp.Type))
                    {
                        returnConsumers.Add(consumer);
                    }
                }
            }
            return returnConsumers as HashSet<T> ?? new HashSet<T>();
        }

        return new HashSet<T>();
    }
}
