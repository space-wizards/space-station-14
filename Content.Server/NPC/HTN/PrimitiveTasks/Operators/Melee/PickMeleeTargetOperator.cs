using JetBrains.Annotations;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Selects a target for melee.
/// </summary>
[MeansImplicitUse]
public sealed class PickMeleeTargetOperator : NPCCombatOperator
{
    protected override float GetRating(NPCBlackboard blackboard, EntityUid uid, EntityUid existingTarget, float distance, bool canMove, EntityQuery<TransformComponent> xformQuery)
    {
        var rating = 0f;

        if (existingTarget == uid)
        {
            rating += 2f;
        }

        if (distance > 0f)
            rating += 50f / distance;

        return rating;
    }
}
