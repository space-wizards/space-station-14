using Content.Server.Hands.Systems;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the NPC's weapon needs to be activated.
/// Returns true if the weapon has ItemToggleComponent and is not activated.
/// </summary>
public sealed partial class NeedToActivateWeaponPrecondition : HTNPrecondition
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

        if (!_entManager.TryGetComponent<ItemToggleComponent>(heldEntity, out var itemToggle))
            return false;

        return !itemToggle.Activated && itemToggle.OnUse;
    }
}
