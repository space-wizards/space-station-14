using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
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
public sealed partial class ActiveTelecomsEntityChatCondition : EntityChatCondition
{

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override HashSet<EntityUid> FilterConsumers(HashSet<EntityUid> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);
        var returnConsumers = new HashSet<EntityUid>();

        if (!channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity) ||
            !channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel) ||
            !_prototypeManager.TryIndex((string)radioChannel, out RadioChannelPrototype? radioPrototype) ||
            !_entityManager.TryGetComponent<TransformComponent>((EntityUid)senderEntity, out var sourceTransform))
            return new HashSet<EntityUid>();

        var activeServer = radioPrototype.LongRange;

        Logger.Debug("1: " + (string)radioChannel);

        if (activeServer == false)
        {
            Logger.Debug("2: " + activeServer.ToString());
            var servers = _entityManager.EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
            var sourceMapId = sourceTransform.MapID;
            foreach (var (_, keys, power, transform) in servers)
            {
                Logger.Debug("3: ");
                if (transform.MapID == sourceMapId &&
                    power.Powered &&
                    keys.Channels.Contains(radioPrototype.ID))
                {
                    Logger.Debug("4: ");
                    activeServer = true;
                }
            }
        }

        if (activeServer)
            returnConsumers = consumers;

        return returnConsumers;
    }
}
