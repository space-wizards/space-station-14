using Content.Server.NPC.Combat;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Melee;

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
        HTNOperatorStatus status = HTNOperatorStatus.Continuing;

        if (_entManager.TryGetComponent<NPCMeleeCombatComponent>(owner, out var combat))
        {
            switch (combat.Status)
            {
                case CombatStatus.TargetNormal:
                    status = HTNOperatorStatus.Continuing;
                    break;
                case CombatStatus.TargetCrit:
                case CombatStatus.TargetDead:
                    status = HTNOperatorStatus.Finished;
                    break;
                case CombatStatus.TargetUnreachable:
                case CombatStatus.NoWeapon:
                    status = HTNOperatorStatus.Failed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (status != HTNOperatorStatus.Continuing)
        {
            _entManager.RemoveComponent<NPCMeleeCombatComponent>(owner);
        }

        return status;
    }
}
