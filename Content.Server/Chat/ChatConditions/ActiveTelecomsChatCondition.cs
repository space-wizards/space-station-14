using Content.Server.Power.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer is on a map with an active server, if necessary.
/// </summary>
[DataDefinition]
public sealed partial class ActiveTelecomsChatCondition : EntityChatConditionBase
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext chatContext)
    {
        if (!chatContext.TryGet<string>(DefaultChannelParameters.RadioChannel, out var radioChannel))
            return false;

        IoCManager.InjectDependencies(this);

        if (
            !_prototypeManager.TryIndex(radioChannel, out RadioChannelPrototype? radioPrototype) ||
            !_entityManager.TryGetComponent<TransformComponent>(subjectEntity, out var sourceTransform)
        )
            return false;

        if (radioPrototype.LongRange)
            return true;

        var servers = _entityManager.EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        var sourceMapId = sourceTransform.MapID;
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == sourceMapId &&
                power.Powered &&
                keys.Channels.Contains(radioPrototype.ID))
            {
                return true;
            }
        }

        return false;
    }
}
