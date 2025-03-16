using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer is alive and above crit; does not check for consciousness e.g. sleeping.
/// </summary>
[DataDefinition]
public sealed partial class IsAboveCritChatCondition : EntityChatConditionBase
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext chatContext)
    {
        IoCManager.InjectDependencies(this);

        if (_entitySystem.TryGetEntitySystem<MobStateSystem>(out var mobStateSystem))
        {
            return !mobStateSystem.IsIncapacitated(subjectEntity);
        }

        return false;
    }
}
