using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed class EatTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private FoodSystem _food = default!;

    /// <summary>
    /// Target entity to eat.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _food = sysManager.GetEntitySystem<FoodSystem>();
    }

    public override void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.Shutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<FoodComponent>(target, out var food))
            return HTNOperatorStatus.Failed;

        // this doesn't check if the doafter actually succeeds or not (nor wait for the eating to be done), but
        if (_food.TryFeed(owner, owner, target, food))
            return HTNOperatorStatus.Finished;

        return HTNOperatorStatus.Failed;
    }
}
