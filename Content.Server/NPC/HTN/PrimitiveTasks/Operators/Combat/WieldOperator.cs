using Content.Server.Hands.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Operator that wields a weapon.
/// </summary>
public sealed partial class WieldOperator : HTNOperator
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

        if (!_entManager.TryGetComponent<WieldableComponent>(weaponUid, out var wieldable) || wieldable.Wielded)
            return HTNOperatorStatus.Failed;

        var wieldableSystem = _entManager.System<SharedWieldableSystem>();
        var success = wieldableSystem.TryWield(weaponUid.Value, wieldable, owner);

        return success ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;
    }
}
