using Content.Server.Hands.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the NPC can wield the current weapon.
/// </summary>
public sealed partial class CanWieldPrecondition : HTNPrecondition
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

        if (!_entManager.TryGetComponent<WieldableComponent>(heldEntity, out var wieldable) ||
            wieldable.Wielded)
            return false;

        var wieldableSystem = _entManager.System<SharedWieldableSystem>();

        return wieldableSystem.CanWield(heldEntity.Value, wieldable, owner, quiet: true);
    }
}
