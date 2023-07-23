using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if the active hand is unoccupied.
/// </summary>
public sealed class ActiveHandFreePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        // TODO: Make this also effect applyable.
        if (!blackboard.TryGetValue<Hand>(NPCBlackboard.ActiveHand, out var hand, _entManager))
        {
            return false;
        }

        return hand.IsEmpty;
    }
}
