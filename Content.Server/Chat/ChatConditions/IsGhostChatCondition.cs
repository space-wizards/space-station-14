using Content.Shared.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class IsGhostChatCondition : ChatCondition
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        IoCManager.InjectDependencies(this);

        return _entityManager.HasComponent<GhostComponent>(subjectEntity);
    }

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext channelParameters)
    {
        IoCManager.InjectDependencies(this);

        return _entityManager.HasComponent<GhostComponent>(subjectSession.AttachedEntity);
    }
}
