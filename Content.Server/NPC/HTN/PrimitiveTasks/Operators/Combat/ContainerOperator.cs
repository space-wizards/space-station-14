using Robust.Server.Containers;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed partial class ContainerOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private ContainerSystem _container = default!;
    private EntityQuery<TransformComponent> _transformQuery;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _container = sysManager.GetEntitySystem<ContainerSystem>();
        _transformQuery = _entManager.GetEntityQuery<TransformComponent>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_container.TryGetOuterContainer(owner, _transformQuery.GetComponent(owner), out var outerContainer) && outerContainer == null)
            return;

        var target = outerContainer.Owner;
        blackboard.SetValue(TargetKey, target);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }
}
