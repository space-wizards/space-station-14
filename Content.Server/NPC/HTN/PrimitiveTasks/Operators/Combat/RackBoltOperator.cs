using Content.Server.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Operator that racks the bolt of a gun with ChamberMagazineAmmoProviderComponent.
/// </summary>
public sealed partial class RackBoltOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var gunUid))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(gunUid, out var chamberMagazine))
            return HTNOperatorStatus.Failed;

        var gunSystem = _entManager.System<SharedGunSystem>();

        if (chamberMagazine.CanRack)
        {
            gunSystem.UseChambered(gunUid.Value, chamberMagazine, owner);
        }
        else if (chamberMagazine.BoltClosed is not null)
        {
            gunSystem.ToggleBolt(gunUid.Value, chamberMagazine, owner);
        }
        else
        {
            return HTNOperatorStatus.Failed;
        }

        return HTNOperatorStatus.Finished;
    }
}
