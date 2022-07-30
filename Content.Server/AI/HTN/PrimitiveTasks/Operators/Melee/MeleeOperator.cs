using Content.Server.AI.Components;

namespace Content.Server.AI.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Attacks the specified key in melee combat.
/// </summary>
public sealed class MeleeOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [ViewVariables, DataField("targetKey", required: true)]
    public string TargetKey = default!;

    // TODO: Need a key for what we're using

    // Like movement we add a component and pass it off to the dedicated system.

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var melee = _entManager.EnsureComponent<NPCMeleeCombatComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
        melee.Target = blackboard.GetValue<EntityUid>(TargetKey);
    }

    public override void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.Shutdown(blackboard, status);
        _entManager.RemoveComponent<NPCMeleeCombatComponent>(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        base.Update(blackboard, frameTime);
        // TODO:
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _entManager.RemoveComponent<NPCMeleeCombatComponent>(owner);

        if (_entManager.HasComponent<NPCMeleeCombatComponent>(owner))
            return HTNOperatorStatus.Continuing;

        return HTNOperatorStatus.Finished;
    }
}
