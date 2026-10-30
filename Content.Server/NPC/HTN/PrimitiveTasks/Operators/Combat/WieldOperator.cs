using Content.Shared.Hands.EntitySystems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

/// <summary>
/// Operator that wields or unwields a weapon.
/// </summary>
public sealed partial class WieldOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedWieldableSystem _wieldable = default!;

    /// <summary>
    /// If true, tries to wield the item. If false, tries to unwield it.
    /// </summary>
    [DataField]
    public bool Wield = true;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager))
            return HTNOperatorStatus.Failed;

        if (!_handsSystem.TryGetHeldItem(owner, activeHand, out var weaponUid))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<WieldableComponent>(weaponUid, out var wieldable))
            return HTNOperatorStatus.Failed;

        if (Wield)
        {
            if (wieldable.Wielded)
                return HTNOperatorStatus.Failed;

            return _wieldable.TryWield((weaponUid.Value, wieldable), owner)
                ? HTNOperatorStatus.Finished
                : HTNOperatorStatus.Failed;
        }

        if (!wieldable.Wielded)
            return HTNOperatorStatus.Failed;

        return _wieldable.TryUnwield((weaponUid.Value, wieldable), owner)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}
