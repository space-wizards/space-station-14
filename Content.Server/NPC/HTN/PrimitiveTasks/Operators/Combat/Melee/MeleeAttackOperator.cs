using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat.Melee;

/// <summary>
/// Something between <see cref="MeleeOperator"/> and <see cref="InteractWithOperator"/>, this operator causes the NPC
/// to attempt a SINGLE <see cref="SharedMeleeWeaponSystem.AttemptLightAttack">melee attack</see> on the specified
/// <see cref="TargetKey">target</see>.
/// </summary>
public sealed partial class MeleeAttackOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SharedMeleeWeaponSystem _melee;

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _melee = sysManager.GetEntitySystem<SharedMeleeWeaponSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        ExitCombatMode(blackboard);
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        base.PlanShutdown(blackboard);

        ExitCombatMode(blackboard);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<CombatModeComponent>(owner, out var combatMode) ||
            !_melee.TryGetWeapon(owner, out var weaponUid, out var weapon))
        {
            return HTNOperatorStatus.Failed;
        }

        _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, true, combatMode);


        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) ||
            !_melee.AttemptLightAttack(owner, weaponUid, weapon, target))
        {
            return HTNOperatorStatus.Continuing;
        }

        return HTNOperatorStatus.Finished;
    }

    private void ExitCombatMode(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, false);
    }
}
