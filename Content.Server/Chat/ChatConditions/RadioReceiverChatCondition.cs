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

    [Dependency] private readonly IEntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (!channelParameters.TryGetValue(DefaultChannelParameters.RadioChannel, out var radioChannel))
            return false;

        if (_entityManager.TryGetComponent<WearingHeadsetComponent>(subjectEntity, out var headsetComponent))
        {
            if (_entityManager.TryGetComponent(headsetComponent.Headset, out EncryptionKeyHolderComponent? keys) &&
                keys.Channels.Contains((string)radioChannel))
            {
                return true;
            }
        }
        else if (_entityManager.TryGetComponent<IntrinsicRadioReceiverComponent>(subjectEntity, out var intrinsicRadioTransmitterComponent) &&
                 _entityManager.TryGetComponent<ActiveRadioComponent>(subjectEntity, out var activeRadioComponent))
        {
            if (activeRadioComponent.Channels.Contains((string)radioChannel))
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
