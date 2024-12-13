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
/// Checks if the consumer has a headset with the given channel key
/// </summary>
[DataDefinition]
public sealed partial class WearingHeadsetEntityChatCondition : EntityChatCondition
{

    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override HashSet<EntityUid> FilterConsumers(HashSet<EntityUid> consumers, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);
        var returnConsumers = new HashSet<EntityUid>();

        if (!channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity) ||
            !channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel))
            return new HashSet<EntityUid>();

        foreach (var consumer in consumers)
        {
            if (_entityManager.TryGetComponent<WearingHeadsetComponent>((EntityUid)senderEntity, out var headsetComponent))
            {
                if (_entityManager.TryGetComponent(headsetComponent.Headset, out EncryptionKeyHolderComponent? keys) &&
                    keys.Channels.Contains((string)radioChannel))

                {
                    returnConsumers.Add(consumer);
                }
            }
        }

        return returnConsumers;
    }
}
