using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

/// <summary>
/// Operator that racks the bolt of a gun with <see cref="ChamberMagazineAmmoProviderComponent"/>.
/// </summary>
public sealed partial class RackBoltOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedGunSystem _gunSystem = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        if (!_handsSystem.TryGetHeldItem(owner, activeHand, out var gunUid))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(gunUid, out var chamberMagazine))
            return HTNOperatorStatus.Failed;

        _gunSystem.UseChambered(gunUid.Value, chamberMagazine, owner);

        return HTNOperatorStatus.Finished;
    }
}
