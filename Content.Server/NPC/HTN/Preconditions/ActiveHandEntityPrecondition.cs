using Content.Shared.Hands.EntitySystems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if an entity is held in the active hand.
/// </summary>
public sealed partial class ActiveHandEntityPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue(NPCBlackboard.Owner, out EntityUid owner, _entManager) ||
            !blackboard.TryGetValue(NPCBlackboard.ActiveHand, out string? activeHand, _entManager))
        {
            return false;
        }

        return !_handsSystem.HandIsEmpty(owner, activeHand);
    }
}
