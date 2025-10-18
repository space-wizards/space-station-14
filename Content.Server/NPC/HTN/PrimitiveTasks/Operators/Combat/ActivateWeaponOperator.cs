using Content.Server.Hands.Systems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Operator that activates a weapon with ItemToggleComponent.
/// </summary>
public sealed partial class ActivateWeaponOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        var handsSystem = _entManager.System<HandsSystem>();
        if (!handsSystem.TryGetHeldItem(owner, activeHand, out var weaponUid))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<ItemToggleComponent>(weaponUid, out var itemToggle))
            return HTNOperatorStatus.Failed;

        var itemToggleSystem = _entManager.System<ItemToggleSystem>();
        var success = itemToggleSystem.TryActivate(weaponUid.Value, owner);

        return success ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
