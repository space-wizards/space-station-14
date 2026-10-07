using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed partial class UnPullOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    private EntityQuery<PullableComponent> _pullableQuery;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    public override void Initialize(IDependencyCollection deps)
    {
        base.Initialize(deps);
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
