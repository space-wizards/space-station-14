using System.Linq;
using Content.Server.Radio.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer has a method to transmit radio messages
/// </summary>
[DataDefinition]
public sealed partial class RadioReceiverChatCondition : ChatCondition
{
    public override Type? ConsumerType { get; set; } = typeof(EntityUid);

    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        if (consumers is HashSet<EntityUid> entityConsumers)
        {
            IoCManager.InjectDependencies(this);

            if (!channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel))
                return new HashSet<T>();

            var filteredEntities = new HashSet<EntityUid>();

            foreach (var consumer in entityConsumers)
            {
                if (_entityManager.TryGetComponent<WearingHeadsetComponent>(consumer,
                        out var headsetComponent))
                {
                    if (_entityManager.TryGetComponent(headsetComponent.Headset,
                            out EncryptionKeyHolderComponent? keys) &&
                        keys.Channels.Contains((string)radioChannel))
                    {
                        filteredEntities.Add(consumer);
                    }
                }
                else if (_entityManager.TryGetComponent<IntrinsicRadioReceiverComponent>(consumer,
                             out var intrinsicRadioTransmitterComponent) &&
                         _entityManager.TryGetComponent<ActiveRadioComponent>(consumer,
                             out var activeRadioComponent))
                {
                    if (activeRadioComponent.Channels.Contains((string)radioChannel))
                    {
                        filteredEntities.Add(consumer);
                    }
                }
            }

            return filteredEntities as HashSet<T> ?? new HashSet<T>();
        }

        return new HashSet<T>();
    }
}
