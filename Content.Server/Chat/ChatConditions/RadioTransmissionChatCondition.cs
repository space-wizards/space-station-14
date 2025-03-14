using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio.Components;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer has a method to transmit radio messages
/// </summary>
[DataDefinition]
public sealed partial class RadioTransmissionChatCondition : EntityChatConditionBase
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext chatContext)
    {
        if (!chatContext.TryGet<string>(DefaultChannelParameters.RadioChannel, out var radioChannel))
            return false;

        IoCManager.InjectDependencies(this);

        if (_entityManager.TryGetComponent<WearingHeadsetComponent>(subjectEntity, out var headsetComponent))
        {
            return _entityManager.TryGetComponent(headsetComponent.Headset, out EncryptionKeyHolderComponent? keys)
                   && keys.Channels.Contains(radioChannel);
        }

        if (_entityManager.TryGetComponent<IntrinsicRadioTransmitterComponent>(subjectEntity, out var intrinsicRadioTransmitterComponent))
            return intrinsicRadioTransmitterComponent.Channels.Contains(radioChannel);

        return false;
    }
}
