using Content.Shared.Chat;
using Content.Shared.Ghost;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class IsGhostChatCondition : EntityChatConditionBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext chatContext)
    {
        IoCManager.InjectDependencies(this);

        return _entityManager.HasComponent<GhostComponent>(subjectEntity);
    }
}
