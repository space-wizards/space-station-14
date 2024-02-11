using Content.Shared.Hands.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if an entity is held in the active hand.
/// </summary>
public sealed partial class ActiveHandEntityPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue(NPCBlackboard.ActiveHand, out Hand? activeHand, _entManager))
        {
            return false;
        }

        return activeHand.HeldEntity != null;
    }
}
