using Content.Server.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the NPC's gun needs bolt racking - either bolt is open OR bolt is closed but chamber is empty.
/// Returns true if gun needs racking to prepare for firing.
/// </summary>
public sealed partial class NeedToRackBoltPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return false;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var heldEntity))
            return false;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(heldEntity, out var chamberMagazine))
            return false;

        if (!chamberMagazine.CanRack)
            return false;

        var gunSystem = _entManager.System<SharedGunSystem>();
        var chamberEntity = gunSystem.GetChamberEntity(heldEntity.Value);
        bool hasRoundInChamber = chamberEntity is not null;

        return chamberMagazine.BoltClosed == false ||
               chamberMagazine.BoltClosed == true && !hasRoundInChamber ||
               chamberMagazine.BoltClosed == null && !hasRoundInChamber;
    }
}
