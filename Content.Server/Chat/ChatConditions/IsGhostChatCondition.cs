using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.ChatConditions;

[DataDefinition]
public sealed partial class IsGhostChatCondition : ChatCondition
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext channelParameters)
    {
        IoCManager.InjectDependencies(this);

        Logger.Debug("weh");
        return _entityManager.HasComponent<GhostComponent>(subjectEntity);
    }

    protected override bool Check(ICommonSession subjectSession, ChatMessageContext channelParameters)
    {
        IoCManager.InjectDependencies(this);

        Logger.Debug("wah");
        return _entityManager.HasComponent<GhostComponent>(subjectSession.AttachedEntity);
    }
}
