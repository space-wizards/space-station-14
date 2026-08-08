using Content.Shared.Chat;
using Content.Shared.Silicons.Bots;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class MedibotInjectOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private MedibotSystem _medibot = default!;

    /// <summary>
    /// Target entity to inject.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        // TODO: Wat
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) || _entMan.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entMan.TryGetComponent<MedibotComponent>(owner, out var botComp))
            return HTNOperatorStatus.Failed;

        if (!_medibot.CheckInjectable((owner, botComp), target) || !_medibot.TryInject((owner, botComp), target))
            return HTNOperatorStatus.Failed;

        _chat.TrySendInGameICMessage(owner, Loc.GetString("medibot-finish-inject"), InGameICChatType.Speak, hideChat: true, hideLog: true);

        return HTNOperatorStatus.Finished;
    }
}
