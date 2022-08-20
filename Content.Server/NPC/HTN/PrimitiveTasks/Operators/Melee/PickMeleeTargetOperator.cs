namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Selects a target for melee.
/// </summary>
public sealed class PickMeleeTargetOperator : NPCCombatOperator
{
    protected override float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget, bool canMove, EntityQuery<TransformComponent> xformQuery)
    {
        var rating = 0f;

        if (existingTarget == uid)
        {
            rating += 3f;
        }

        return rating;
    }
}
