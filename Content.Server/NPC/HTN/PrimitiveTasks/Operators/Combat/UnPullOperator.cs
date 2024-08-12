using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed partial class UnPullOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private PullingSystem _pulling = default!;
    private ActionBlockerSystem _actionBlocker = default!;

    private EntityQuery<PullableComponent> _pullableQuery;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _actionBlocker = sysManager.GetEntitySystem<ActionBlockerSystem>();
        _pulling = sysManager.GetEntitySystem<PullingSystem>();
        _pullableQuery = _entManager.GetEntityQuery<PullableComponent>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_actionBlocker.CanInteract(owner, owner)) //prevents handcuffed monkeys from pulling etc.
            _pulling.TryStopPull(owner, _pullableQuery.GetComponent(owner), owner);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }
}
