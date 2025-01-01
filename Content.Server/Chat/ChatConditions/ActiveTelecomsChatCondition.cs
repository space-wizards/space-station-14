using Content.Server.Power.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Microsoft.Extensions.Logging;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer is on a map with an active server, if necessary.
/// </summary>
[DataDefinition]
public sealed partial class ActiveTelecomsChatCondition : ChatCondition
{
    public override Type? ConsumerType { get; set; } = null;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (!channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity) ||
            !channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel) ||
            !_prototypeManager.TryIndex((string)radioChannel, out RadioChannelPrototype? radioPrototype) ||
            !_entityManager.TryGetComponent<TransformComponent>((EntityUid)senderEntity, out var sourceTransform))
            return new HashSet<T>();

        var activeServer = radioPrototype.LongRange;

        if (activeServer == false)
        {
            var servers = _entityManager.EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
            var sourceMapId = sourceTransform.MapID;
            foreach (var (_, keys, power, transform) in servers)
            {
                if (transform.MapID == sourceMapId &&
                    power.Powered &&
                    keys.Channels.Contains(radioPrototype.ID))
                {
                    activeServer = true;
                }
            }
        }

        return activeServer ? consumers : new HashSet<T>();
    }
}
