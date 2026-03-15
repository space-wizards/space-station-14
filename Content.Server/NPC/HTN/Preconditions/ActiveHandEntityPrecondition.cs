using Content.Server.Hands.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if an entity is held in the active hand.
/// </summary>
public sealed partial class ActiveHandEntityPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue(NPCBlackboard.Owner, out EntityUid owner, _entManager) ||
            !blackboard.TryGetValue(NPCBlackboard.ActiveHand, out string? activeHand, _entManager))
        {
            return false;
        }

        return !_entManager.System<HandsSystem>().HandIsEmpty(owner, activeHand);
    }
}
