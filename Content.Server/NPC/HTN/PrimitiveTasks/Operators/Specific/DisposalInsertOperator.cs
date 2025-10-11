using Content.Server.Disposal.Unit;
using Content.Shared.Disposal.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class DisposalInsertOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private DisposalUnitSystem _disposalSystem = default!;

    /// <summary>
    /// Target entity to flush
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target disposal bin entity
    /// </summary>
    [DataField(required: true)]
    public string DisposalTargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _disposalSystem = sysManager.GetEntitySystem<DisposalUnitSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
        blackboard.Remove<EntityUid>(DisposalTargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!blackboard.TryGetValue<EntityUid>(DisposalTargetKey, out var disposalUnitTarget, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entManager.HasComponent<DisposalUnitComponent>(disposalUnitTarget))
            return HTNOperatorStatus.Failed;

        if (!_disposalSystem.TryInsert(disposalUnitTarget, target, owner))
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;

    }
}
