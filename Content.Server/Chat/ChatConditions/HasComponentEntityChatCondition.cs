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
public sealed partial class HasComponentEntityChatCondition : EntityChatCondition
{

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    [DataField]
    public string? Component;

    public override HashSet<EntityUid> FilterConsumers(HashSet<EntityUid> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        var returnConsumers = new HashSet<EntityUid>();

        if (Component != null)
        {
            foreach (var consumer in consumers)
            {
                var comp = _componentFactory.GetRegistration(Component, true);
                if (_entityManager.HasComponent(consumer, comp.Type))
                {
                    returnConsumers.Add(consumer);
                }
            }
        }

        return returnConsumers;
    }
}
