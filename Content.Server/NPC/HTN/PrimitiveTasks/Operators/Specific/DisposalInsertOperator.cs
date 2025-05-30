using Content.Server.Chat.Systems;
using Content.Server.Disposal.Unit;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Containers;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class DisposalInsertOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private ChatSystem _chat = default!;
    private DisposalUnitSystem _disposalSystem = default!;
    private SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <summary>
    /// Target entity to flush
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target disposal bin entity
    /// </summary>
    [DataField("disposalTargetKey", required: true)]
    public string DisposalTargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _disposalSystem = sysManager.GetEntitySystem<DisposalUnitSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var disposalUnitQuery = _entManager.GetEntityQuery<DisposalUnitComponent>();


        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!blackboard.TryGetValue<EntityUid>(DisposalTargetKey, out var disposalTarget, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!disposalUnitQuery.TryGetComponent(disposalTarget, out var disposalComp))
            return HTNOperatorStatus.Failed;

        var container = _container.GetContainer(disposalTarget, disposalComp.Container.ID);

        _container.Insert(target, container);

        //_disposalSystem.TryInsert(disposalTarget, target, null);

        //_disposalSystem.DoInsertDisposalUnit(disposalTarget, target, owner);

        return HTNOperatorStatus.Finished;

    }
}
