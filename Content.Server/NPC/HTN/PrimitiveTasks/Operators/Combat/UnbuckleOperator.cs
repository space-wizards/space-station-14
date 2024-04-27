using Content.Server.Buckle.Systems;
using Content.Shared.Buckle.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed partial class UnbuckleOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private BuckleSystem _buckle = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _buckle = sysManager.GetEntitySystem<BuckleSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entManager.TryGetComponent<BuckleComponent>(owner, out var buckle) || !buckle.Buckled)
            return;

        _buckle.TryUnbuckle(owner, owner, true, buckle);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }
}
