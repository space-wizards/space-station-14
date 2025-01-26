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
public sealed partial class RadioTransmissionChatCondition : ChatCondition
{

    [Dependency] private readonly IEntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        if (!channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity) ||
            !channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel))
            return false;

        IoCManager.InjectDependencies(this);

        if (_entityManager.TryGetComponent<WearingHeadsetComponent>((EntityUid)senderEntity, out var headsetComponent))
        {
            if (_entityManager.TryGetComponent(headsetComponent.Headset, out EncryptionKeyHolderComponent? keys) &&
                keys.Channels.Contains((string)radioChannel))
            {
                return true;
            }
        }
        else if (_entityManager.TryGetComponent<IntrinsicRadioTransmitterComponent>((EntityUid)senderEntity, out var intrinsicRadioTransmitterComponent))
        {
            if (intrinsicRadioTransmitterComponent.Channels.Contains((string)radioChannel))
            {
                return true;
            }
        }

        return false;
    }

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext channelParameters)
    {
        return false;
    }
}
