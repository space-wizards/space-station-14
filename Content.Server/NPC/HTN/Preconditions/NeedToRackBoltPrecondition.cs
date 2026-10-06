using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the NPC's gun needs bolt racking - either bolt is open OR bolt is closed but chamber is empty.
/// Returns true if gun needs racking to prepare for firing.
/// </summary>
public sealed partial class NeedToRackBoltPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedGunSystem _gunSystem = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return false;

        if (!_handsSystem.TryGetHeldItem(owner, activeHand, out var heldEntity))
            return false;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(heldEntity, out var chamberMagazine))
            return false;

        if (!chamberMagazine.CanRack)
            return false;

        var chamberEntity = _gunSystem.GetChamberEntity(heldEntity.Value);
        bool hasRoundInChamber = chamberEntity is not null;

        return chamberMagazine.BoltClosed == false || !hasRoundInChamber;
    }
}
