using Content.Server.Interaction;
using Content.Shared.Timing;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed class InteractWithOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_entManager.System<UseDelaySystem>().ActiveDelay(owner) ||
            !blackboard.TryGetValue<EntityUid>(TargetKey, out var moveTarget, _entManager) ||
            !_entManager.TryGetComponent<TransformComponent>(moveTarget, out var targetXform))
        {
            return HTNOperatorStatus.Continuing;
        }

        _entManager.System<InteractionSystem>().UserInteraction(owner, targetXform.Coordinates, moveTarget);

        return HTNOperatorStatus.Finished;
    }
}
