using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class RangeChatCondition : ChatCondition
{
    /// <summary>
    /// The minimum range to meet this condition; inclusive.
    /// </summary>
    [DataField]
    public float MinimumRange = 0f;

    /// <summary>
    /// The maximum range to meet this condition; exclusive.
    /// </summary>
    [DataField]
    public float MaximumRange = 5f;

    [Dependency] private readonly IEntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        if (!channelParameters.TryGet<EntityUid>(DefaultChannelParameters.SenderEntity, out var senderEntity))
            return false;

        IoCManager.InjectDependencies(this);

        if (!_entityManager.TryGetComponent<TransformComponent>(senderEntity, out var sourceTransform)
            || !_entityManager.TryGetComponent<TransformComponent>(subjectEntity, out var transform)
        )
            return false;

        if (transform.MapID != sourceTransform.MapID)
            return false;

        // If you wanted to do something like a hard-of-hearing trait, our hearing extension component,
        // this is probably where you'd check for it.
        // Even if they are a ghost hearer, in some situations we still need the range
        return sourceTransform.Coordinates.TryDistance(_entityManager, transform.Coordinates, out var distance)
               && distance < MaximumRange
               && distance >= MinimumRange;
    }

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext channelParameters)
    {
        return false;
    }
}
