using Content.Server.Disposal.Unit;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class DisposalInsertOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private DisposalUnitSystem _disposalSystem = default!;

    /// <summary>
    /// Target entity to dispose of
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _disposalSystem = sysManager.GetEntitySystem<DisposalUnitSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (_disposalSystem.CanInsert(target.Id

        return HTNOperatorStatus.Finished;
    }
}
